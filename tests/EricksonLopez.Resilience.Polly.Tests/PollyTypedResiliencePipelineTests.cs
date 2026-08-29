// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Pipelines;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using global::Polly;
using PollyCircuitBreaker = global::Polly.CircuitBreaker;
using EcoBuilder = EricksonLopez.Resilience.Builder.ResiliencePipelineBuilder;
using EcoTypedBuilder = EricksonLopez.Resilience.Builder.ResiliencePipelineBuilder<string>;
using Xunit;

namespace EricksonLopez.Resilience.Polly.Tests;

public sealed class PollyTypedResiliencePipelineTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceName_ThrowsArgumentException(string? invalidName)
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder<string>().Build();
        var act = () => new PollyResiliencePipeline<string>(invalidName!, pollyPipeline);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullPipeline_ThrowsArgumentNullException()
    {
        var act = () => new PollyResiliencePipeline<string>("valid-name", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder<int>().Build();
        var pipeline = new PollyResiliencePipeline<int>("typed-p", pollyPipeline);
        pipeline.Name.Should().Be("typed-p");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperationOrContext_ThrowsArgumentNullException()
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder<string>().Build();
        var pipeline = new PollyResiliencePipeline<string>("p", pollyPipeline);
        var context = ResilienceContext.Create("p");

        Func<Task> act1 = async () => await pipeline.ExecuteAsync(null!, context);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act2 = async () => await pipeline.ExecuteAsync(ctx => ValueTask.FromResult("ok"), null!);
        await act2.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act3 = async () => await pipeline.ExecuteAsync((Func<CancellationToken, ValueTask<string>>)null!);
        await act3.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Fallback_OnException_ReturnsFallbackValue()
    {
        // Arrange
        var builder = new EcoTypedBuilder("test-fallback-ex");
        var fallbackInvoked = false;

        builder.AddFallback(opt =>
        {
            opt.ShouldHandleException = ex => ex is IOException;
            opt.FallbackAction = ctx => ValueTask.FromResult("Cached-Fallback-Value");
            opt.OnFallback = ctx =>
            {
                fallbackInvoked = true;
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync(ct => throw new IOException("Disk error"));

        // Assert
        result.Should().Be("Cached-Fallback-Value");
        fallbackInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task Fallback_OnMatchingResult_ReturnsFallbackValue()
    {
        // Arrange
        var builder = new EcoTypedBuilder("test-fallback-res");

        builder.AddFallback(opt =>
        {
            opt.ShouldHandleResult = res => res == "Degraded";
            opt.FallbackAction = ctx => ValueTask.FromResult("Alternative-Result");
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Degraded"));

        // Assert
        result.Should().Be("Alternative-Result");
    }

    [Fact]
    public async Task Fallback_WhenOperationSucceeds_ReturnsOriginalResult()
    {
        // Arrange
        var builder = new EcoTypedBuilder("test-fallback-ok");

        builder.AddFallback(opt =>
        {
            opt.ShouldHandleException = ex => true;
            opt.FallbackAction = ctx => ValueTask.FromResult("Fallback");
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Original-Success"));

        // Assert
        result.Should().Be("Original-Success");
    }

    [Fact]
    public async Task Fallback_WhenFallbackActionIsNull_ReturnsDefault()
    {
        // Arrange
        var builder = new EcoTypedBuilder("test-fallback-null-action");
        var field = typeof(EcoTypedBuilder).GetField("_strategies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var strategies = (System.Collections.Generic.List<EricksonLopez.Resilience.Options.ResilienceStrategyOptions>)field.GetValue(builder)!;
        strategies.Add(new FallbackStrategyOptions<string>
        {
            ShouldHandleException = ex => true,
            FallbackAction = null
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync(ct => throw new InvalidOperationException("Fail"));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Timeout_ThrowsResilienceTimeoutException()
    {
        // Arrange
        var builder = new EcoTypedBuilder("typed-timeout");
        builder.AddTimeout(TimeSpan.FromMilliseconds(50));
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(500, ct);
                return "late";
            });
        };

        // Assert
        var ex = await act.Should().ThrowAsync<ResilienceTimeoutException>();
        ex.Which.Timeout.Should().Be(TimeSpan.FromMilliseconds(50));
        ex.Which.PolicyName.Should().Be("typed-timeout");
    }

    [Fact]
    public async Task CircuitBreaker_ConsecutiveFailures_OpensAndRejectsCalls()
    {
        // Arrange
        var builder = new EcoTypedBuilder("typed-cb");
        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = ex => ex is InvalidOperationException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        for (var i = 0; i < 2; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(ct => throw new InvalidOperationException("Fail"));
            }
            catch (InvalidOperationException)
            {
            }
        }

        Func<Task> act = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult("ok"));
        var ex = await act.Should().ThrowAsync<CircuitBrokenException>();
        ex.Which.PolicyName.Should().Be("typed-cb");
        ex.Which.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task IsolatedCircuitBreaker_ThrowsCircuitBrokenExceptionWithNullRetryAfter()
    {
        // Arrange
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var manualControl = new PollyCircuitBreaker.CircuitBreakerManualControl();
        pollyBuilder.AddCircuitBreaker(new PollyCircuitBreaker.CircuitBreakerStrategyOptions<string>
        {
            ManualControl = manualControl
        });

        var pipeline = new PollyResiliencePipeline<string>("isolated-typed", pollyBuilder.Build());
        await manualControl.IsolateAsync();

        // Act & Assert
        Func<Task> act = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult("test"));
        var ex = await act.Should().ThrowAsync<CircuitBrokenException>();
        ex.Which.PolicyName.Should().Be("isolated-typed");
        ex.Which.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task RateLimiter_Rejection_ThrowsRateLimitRejectedException()
    {
        // Arrange
        var builder = new EcoTypedBuilder("typed-rl");
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = RateLimiterType.FixedWindow;
            opt.PermitLimit = 1;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMinutes(1);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        var r1 = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("first"));
        r1.Should().Be("first");

        Func<Task> act = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult("second"));
        var ex = await act.Should().ThrowAsync<RateLimitRejectedException>();
        ex.Which.PolicyName.Should().Be("typed-rl");
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesContextPropertiesAndCancellationToken()
    {
        // Arrange
        var builder = new EcoTypedBuilder("typed-propagate");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        using var cts = new CancellationTokenSource();

        var inputContext = ResilienceContext.Create("typed-propagate")
            .WithOperationName("OpB")
            .WithCorrelationId("corr-2")
            .WithTenantId("tenant-2");

        ResilienceContext? observedContext = null;

        // Act
        var result = await pipeline.ExecuteAsync(
            ctx =>
            {
                observedContext = ctx;
                return ValueTask.FromResult("done");
            },
            inputContext,
            cts.Token);

        // Assert
        result.Should().Be("done");
        observedContext.Should().NotBeNull();
        observedContext!.PolicyName.Should().Be("typed-propagate");
        observedContext.OperationName.Should().Be("OpB");
        observedContext.CorrelationId.Should().Be("corr-2");
        observedContext.TenantId.Should().Be("tenant-2");
        observedContext.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExplicitTokenIsDefault_UsesContextToken()
    {
        // Arrange
        var builder = new EcoTypedBuilder("typed-token-default-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        using var cts = new CancellationTokenSource();
        var context = ResilienceContext.Create("typed-token-default-policy", cts.Token);

        // Act & Assert
        ResilienceContext? genCtx = null;
        await pipeline.ExecuteAsync(
            ctx =>
            {
                genCtx = ctx;
                return ValueTask.FromResult("val");
            },
            context,
            cancellationToken: default);

        genCtx.Should().NotBeNull();
        genCtx!.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationThrows_FinallyBlockReturnsContextToPool()
    {
        // Arrange
        var builder = new EcoTypedBuilder("typed-throw-return-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var context = ResilienceContext.Create("typed-throw-return-policy");

        // Act
        Func<Task> actGen = async () => await pipeline.ExecuteAsync(
            ctx => throw new InvalidOperationException("typed boom"),
            context);
        await actGen.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_FinallyBlock_ReturnsAndClearsPollyContextProperties()
    {
        global::Polly.ResilienceContext? capturedPollyCtx = null;
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        pollyBuilder.AddStrategy(
            context => new CustomTestStrategy(ctx => capturedPollyCtx = ctx),
            new TestStrategyOptions());
        var pipeline = new PollyResiliencePipeline<string>("typed-finally-check", pollyBuilder.Build());
        var context = ResilienceContext.Create("typed-finally-check");

        await pipeline.ExecuteAsync(ctx => ValueTask.FromResult("ok"), context);

        capturedPollyCtx.Should().NotBeNull();
        var key = new ResiliencePropertyKey<ResilienceContext>("EricksonLopez.Resilience.EcosystemContext");
        capturedPollyCtx!.Properties.TryGetValue(key, out _).Should().BeFalse();
    }

    private sealed class TestStrategyOptions : global::Polly.ResilienceStrategyOptions { }

    private sealed class CustomTestStrategy : global::Polly.ResilienceStrategy
    {
        private readonly Action<global::Polly.ResilienceContext> _onExecute;
        public CustomTestStrategy(Action<global::Polly.ResilienceContext> onExecute) => _onExecute = onExecute;
        protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<global::Polly.ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            global::Polly.ResilienceContext context,
            TState state)
        {
            _onExecute(context);
            return callback(context, state);
        }
    }
}
