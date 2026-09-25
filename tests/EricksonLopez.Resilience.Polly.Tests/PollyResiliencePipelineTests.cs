// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using global::Polly;
using PollyCircuitBreaker = global::Polly.CircuitBreaker;
using EcoBuilder = EricksonLopez.Resilience.Builder.ResiliencePipelineBuilder;
using Xunit;

namespace EricksonLopez.Resilience.Polly.Tests;

public sealed class PollyResiliencePipelineTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceName_ThrowsArgumentException(string? invalidName)
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder().Build();
        var act = () => new PollyResiliencePipeline(invalidName!, pollyPipeline);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullPipeline_ThrowsArgumentNullException()
    {
        var act = () => new PollyResiliencePipeline("valid-name", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SetsNameCorrectly()
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder().Build();
        var pipeline = new PollyResiliencePipeline("pipeline-123", pollyPipeline);
        pipeline.Name.Should().Be("pipeline-123");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperationOrContext_ThrowsArgumentNullException()
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder().Build();
        var pipeline = new PollyResiliencePipeline("p", pollyPipeline);
        var context = ResilienceContext.Create("p");

        Func<Task> act1 = async () => await pipeline.ExecuteAsync<string>(null!, context);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act2 = async () => await pipeline.ExecuteAsync<string>(ctx => ValueTask.FromResult("ok"), null!);
        await act2.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act3 = async () => await pipeline.ExecuteAsync<string>((Func<CancellationToken, ValueTask<string>>)null!);
        await act3.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act4 = async () => await pipeline.ExecuteAsync((Func<ResilienceContext, ValueTask>)null!, context);
        await act4.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act5 = async () => await pipeline.ExecuteAsync(ctx => ValueTask.CompletedTask, null!);
        await act5.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act6 = async () => await pipeline.ExecuteAsync((Func<CancellationToken, ValueTask>)null!);
        await act6.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Retry_TransientFailureThenSuccess_ExecutesAttemptsUntilSuccess()
    {
        // Arrange
        var builder = new EcoBuilder("test-retry");
        var retryCount = 0;

        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(10);
            opt.BackoffType = BackoffType.Constant;
            opt.ShouldHandleException = ex => ex is IOException;
            opt.OnRetry = ctx =>
            {
                retryCount++;
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var attempts = 0;

        // Act
        var result = await pipeline.ExecuteAsync<string>(async ct =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new IOException("Transient network drop");
            }

            return await ValueTask.FromResult("Success");
        });

        // Assert
        result.Should().Be("Success");
        attempts.Should().Be(3);
        retryCount.Should().Be(2);
    }

    [Fact]
    public async Task Retry_PermanentFailure_DoesNotRetry()
    {
        // Arrange
        var builder = new EcoBuilder("test-permanent");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(10);
            opt.ShouldHandleException = ex => ex is IOException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var attempts = 0;

        // Act
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync<string>(async ct =>
            {
                attempts++;
                throw new ArgumentException("Invalid business argument");
            });
        };

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task Timeout_OperationExceedsLimit_ThrowsResilienceTimeoutException()
    {
        // Arrange
        var builder = new EcoBuilder("test-timeout");
        builder.AddTimeout(opt =>
        {
            opt.Timeout = TimeSpan.FromMilliseconds(50);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act & Assert for generic ExecuteAsync
        Func<Task> actGeneric = async () =>
        {
            await pipeline.ExecuteAsync<string>(async ct =>
            {
                await Task.Delay(500, ct);
                return "Too late";
            });
        };
        var exGeneric = await actGeneric.Should().ThrowAsync<ResilienceTimeoutException>();
        exGeneric.Which.Timeout.Should().Be(TimeSpan.FromMilliseconds(50));
        exGeneric.Which.PolicyName.Should().Be("test-timeout");

        // Act & Assert for void ExecuteAsync
        Func<Task> actVoid = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(500, ct);
            });
        };
        var exVoid = await actVoid.Should().ThrowAsync<ResilienceTimeoutException>();
        exVoid.Which.Timeout.Should().Be(TimeSpan.FromMilliseconds(50));
        exVoid.Which.PolicyName.Should().Be("test-timeout");
    }

    [Fact]
    public async Task Cancellation_CallerCancels_ThrowsOperationCanceledExceptionWithoutRetrying()
    {
        // Arrange
        var builder = new EcoBuilder("test-cancel");
        var attempts = 0;

        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 5;
            opt.Delay = TimeSpan.FromMilliseconds(10);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before execution

        // Act
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync<string>(async ct =>
            {
                attempts++;
                ct.ThrowIfCancellationRequested();
                return await ValueTask.FromResult("Done");
            }, cts.Token);
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(0);
    }

    [Fact]
    public async Task CircuitBreaker_ConsecutiveFailures_OpensCircuitAndRejectsFurtherCalls()
    {
        // Arrange
        var builder = new EcoBuilder("test-cb");
        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = ex => ex is InvalidOperationException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act - Trigger consecutive failures to open circuit
        for (var i = 0; i < 2; i++)
        {
            try
            {
                await pipeline.ExecuteAsync<string>(ct => throw new InvalidOperationException("Fail"));
            }
            catch (InvalidOperationException)
            {
                // Expected initial errors
            }
        }

        // Assert - Subsequent execution should be rejected by open circuit
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("ok"));
        };
        var ex = await act.Should().ThrowAsync<CircuitBrokenException>();
        ex.Which.PolicyName.Should().Be("test-cb");
        ex.Which.RetryAfter.Should().NotBeNull();

        // Void overload should also throw CircuitBrokenException
        Func<Task> actVoid = async () =>
        {
            await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask);
        };
        var exVoid = await actVoid.Should().ThrowAsync<CircuitBrokenException>();
        exVoid.Which.PolicyName.Should().Be("test-cb");
    }

    [Fact]
    public async Task IsolatedCircuitBreaker_ThrowsCircuitBrokenExceptionWithNullRetryAfter()
    {
        // Arrange
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        var manualControl = new PollyCircuitBreaker.CircuitBreakerManualControl();
        pollyBuilder.AddCircuitBreaker(new PollyCircuitBreaker.CircuitBreakerStrategyOptions
        {
            ManualControl = manualControl
        });

        var pipeline = new PollyResiliencePipeline("isolated-policy", pollyBuilder.Build());
        await manualControl.IsolateAsync();

        // Act & Assert for generic ExecuteAsync
        Func<Task> actGeneric = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult(123));
        var exGeneric = await actGeneric.Should().ThrowAsync<CircuitBrokenException>();
        exGeneric.Which.PolicyName.Should().Be("isolated-policy");
        exGeneric.Which.RetryAfter.Should().BeNull();

        // Act & Assert for void ExecuteAsync
        Func<Task> actVoid = async () => await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask);
        var exVoid = await actVoid.Should().ThrowAsync<CircuitBrokenException>();
        exVoid.Which.PolicyName.Should().Be("isolated-policy");
        exVoid.Which.RetryAfter.Should().BeNull();
    }

    [Fact]
    public async Task RateLimiter_Rejection_ThrowsRateLimitRejectedException()
    {
        // Arrange
        var builder = new EcoBuilder("test-rl");
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = RateLimiterType.FixedWindow;
            opt.PermitLimit = 1;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMinutes(1);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act 1: consume permit
        await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask);

        // Act 2: generic rejection
        Func<Task> actGeneric = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult("two"));
        var exGeneric = await actGeneric.Should().ThrowAsync<RateLimitRejectedException>();
        exGeneric.Which.PolicyName.Should().Be("test-rl");

        // Act 3: void rejection
        Func<Task> actVoid = async () => await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask);
        var exVoid = await actVoid.Should().ThrowAsync<RateLimitRejectedException>();
        exVoid.Which.PolicyName.Should().Be("test-rl");
    }

    [Fact]
    public async Task ExecuteAsync_PropagatesContextPropertiesAndCancellationToken()
    {
        // Arrange
        var builder = new EcoBuilder("propagate-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        using var cts = new CancellationTokenSource();

        var inputContext = ResilienceContext.Create("propagate-policy")
            .WithOperationName("OpA")
            .WithCorrelationId("corr-1")
            .WithTenantId("tenant-1");

        ResilienceContext? observedContext = null;

        // Act - Generic
        var result = await pipeline.ExecuteAsync(
            ctx =>
            {
                observedContext = ctx;
                return ValueTask.FromResult(99);
            },
            inputContext,
            cts.Token);

        // Assert - Generic
        result.Should().Be(99);
        observedContext.Should().NotBeNull();
        observedContext!.PolicyName.Should().Be("propagate-policy");
        observedContext.OperationName.Should().Be("OpA");
        observedContext.CorrelationId.Should().Be("corr-1");
        observedContext.TenantId.Should().Be("tenant-1");
        observedContext.CancellationToken.Should().Be(cts.Token);

        // Act - Void
        ResilienceContext? observedVoidContext = null;
        await pipeline.ExecuteAsync(
            ctx =>
            {
                observedVoidContext = ctx;
                return ValueTask.CompletedTask;
            },
            inputContext,
            cts.Token);

        // Assert - Void
        observedVoidContext.Should().NotBeNull();
        observedVoidContext!.PolicyName.Should().Be("propagate-policy");
        observedVoidContext.OperationName.Should().Be("OpA");
        observedVoidContext.CorrelationId.Should().Be("corr-1");
        observedVoidContext.TenantId.Should().Be("tenant-1");
        observedVoidContext.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExplicitTokenIsDefault_UsesContextToken()
    {
        // Arrange
        var builder = new EcoBuilder("token-default-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        using var cts = new CancellationTokenSource();
        var context = ResilienceContext.Create("token-default-policy", cts.Token);

        // Act & Assert Generic
        ResilienceContext? genCtx = null;
        await pipeline.ExecuteAsync(
            ctx =>
            {
                genCtx = ctx;
                return ValueTask.FromResult(1);
            },
            context,
            cancellationToken: default);

        genCtx.Should().NotBeNull();
        genCtx!.CancellationToken.Should().Be(cts.Token);

        // Act & Assert Void
        ResilienceContext? voidCtx = null;
        await pipeline.ExecuteAsync(
            ctx =>
            {
                voidCtx = ctx;
                return ValueTask.CompletedTask;
            },
            context,
            cancellationToken: default);

        voidCtx.Should().NotBeNull();
        voidCtx!.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationThrows_FinallyBlockReturnsContextToPool()
    {
        // Arrange
        var builder = new EcoBuilder("throw-return-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var context = ResilienceContext.Create("throw-return-policy");

        // Act Generic
        Func<Task> actGen = async () => await pipeline.ExecuteAsync<string>(
            ctx => throw new InvalidOperationException("boom"),
            context);
        await actGen.Should().ThrowAsync<InvalidOperationException>();

        // Act Void
        Func<Task> actVoid = async () => await pipeline.ExecuteAsync(
            ctx => throw new InvalidOperationException("boom-void"),
            context);
        await actVoid.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_FinallyBlock_ReturnsAndClearsPollyContextProperties()
    {
        global::Polly.ResilienceContext? capturedPollyCtx = null;
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        pollyBuilder.AddStrategy(
            context => new CustomTestStrategy(ctx => capturedPollyCtx = ctx),
            new TestStrategyOptions());
        var pipeline = new PollyResiliencePipeline("finally-check", pollyBuilder.Build());
        var context = ResilienceContext.Create("finally-check");

        // Generic
        await pipeline.ExecuteAsync<string>(ctx => ValueTask.FromResult("ok"), context);

        capturedPollyCtx.Should().NotBeNull();
        var key = new ResiliencePropertyKey<ResilienceContext>("EricksonLopez.Resilience.EcosystemContext");
        capturedPollyCtx!.Properties.TryGetValue(key, out _).Should().BeFalse();

        // Void
        capturedPollyCtx = null;
        await pipeline.ExecuteAsync(ctx => ValueTask.CompletedTask, context);

        capturedPollyCtx.Should().NotBeNull();
        capturedPollyCtx!.Properties.TryGetValue(key, out _).Should().BeFalse();
    }

    [Fact]
    public void PollyExceptionTranslator_TranslatesVariousExceptionsCorrectly()
    {
        var timeoutEx = new global::Polly.Timeout.TimeoutRejectedException("timeout", TimeSpan.FromSeconds(5));
        var translatedTimeout = PollyExceptionTranslator.Translate(timeoutEx, null);
        translatedTimeout.Should().BeOfType<ResilienceTimeoutException>();
        ((ResilienceTimeoutException)translatedTimeout).PolicyName.Should().Be("Resilience");

        var isoEx = new global::Polly.CircuitBreaker.IsolatedCircuitException("isolated");
        var translatedIso = PollyExceptionTranslator.Translate(isoEx, "my-cb");
        translatedIso.Should().BeOfType<CircuitBrokenException>();
        ((CircuitBrokenException)translatedIso).PolicyName.Should().Be("my-cb");

        var genericEx = new InvalidOperationException("generic");
        var translatedGeneric = PollyExceptionTranslator.Translate(genericEx, "policy");
        translatedGeneric.Should().BeSameAs(genericEx);
    }

    [Fact]
    public async Task ExecuteAsync_Generic_PropagatesPropertiesAndAttemptNumber_Bidirectionally()
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder().Build();
        var pipeline = new PollyResiliencePipeline("test-pipeline", pollyPipeline);

        var context = ResilienceContext.Create("test-pipeline")
            .SetProperty("input_prop", "initial_val");
        context.AttemptNumber = 4;

        int observedInitialAttempt = 0;
        ResilienceContext? observedContext = null;
        var result = await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            observedInitialAttempt = ctx.AttemptNumber;
            observedContext = ctx;
            ctx.SetProperty("output_prop", "computed_val");
            ctx.AttemptNumber = 5;
            return 42;
        }, context);

        result.Should().Be(42);
        observedContext.Should().NotBeNull();
        observedInitialAttempt.Should().Be(4);
        observedContext!.Properties["input_prop"].Should().Be("initial_val");

        context.Properties["output_prop"].Should().Be("computed_val");
        context.AttemptNumber.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteAsync_NonGeneric_PropagatesPropertiesAndAttemptNumber_Bidirectionally()
    {
        var pollyPipeline = new global::Polly.ResiliencePipelineBuilder().Build();
        var pipeline = new PollyResiliencePipeline("test-pipeline", pollyPipeline);

        var context = ResilienceContext.Create("test-pipeline")
            .SetProperty("input_prop", "initial_val");
        context.AttemptNumber = 7;

        int observedInitialAttempt = 0;
        ResilienceContext? observedContext = null;
        await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            observedInitialAttempt = ctx.AttemptNumber;
            observedContext = ctx;
            ctx.SetProperty("output_prop", "computed_val");
            ctx.AttemptNumber = 8;
        }, context);

        observedContext.Should().NotBeNull();
        observedInitialAttempt.Should().Be(7);
        observedContext!.Properties["input_prop"].Should().Be("initial_val");

        context.Properties["output_prop"].Should().Be("computed_val");
        context.AttemptNumber.Should().Be(8);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPollyStrategyReplacesEcosystemContext_UsesReplacedEcosystemContext()
    {
        var key = new ResiliencePropertyKey<ResilienceContext>("EricksonLopez.Resilience.EcosystemContext");
        var replacedContext = ResilienceContext.Create("replaced-policy")
            .SetProperty("replaced_key", "replaced_val");
        replacedContext.AttemptNumber = 99;

        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        pollyBuilder.AddStrategy(_ => new CustomTestStrategy(pCtx =>
        {
            pCtx.Properties.Set(key, replacedContext);
        }), new TestStrategyOptions());
        var pipeline = new PollyResiliencePipeline("replaced-test", pollyBuilder.Build());

        var originalContext = ResilienceContext.Create("orig-policy");
        originalContext.AttemptNumber = 1;

        ResilienceContext? observed = null;
        await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            observed = ctx;
            ctx.SetProperty("added_in_op", "val_in_op");
        }, originalContext);

        observed.Should().NotBeNull();
        observed!.AttemptNumber.Should().Be(99);
        observed.Properties["replaced_key"].Should().Be("replaced_val");
        replacedContext.Properties["added_in_op"].Should().Be("val_in_op");
    }

    [Fact]
    public async Task ExecuteAsync_Generic_WhenPollyStrategyReplacesEcosystemContext_UsesReplacedEcosystemContext()
    {
        var key = new ResiliencePropertyKey<ResilienceContext>("EricksonLopez.Resilience.EcosystemContext");
        var replacedContext = ResilienceContext.Create("replaced-policy-gen")
            .SetProperty("replaced_key_gen", "replaced_val_gen");
        replacedContext.AttemptNumber = 77;

        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        pollyBuilder.AddStrategy(_ => new CustomTestStrategy(pCtx =>
        {
            pCtx.Properties.Set(key, replacedContext);
        }), new TestStrategyOptions());
        var pipeline = new PollyResiliencePipeline("replaced-test-gen", pollyBuilder.Build());

        var originalContext = ResilienceContext.Create("orig-policy-gen");
        originalContext.AttemptNumber = 1;

        ResilienceContext? observedGen = null;
        var res = await pipeline.ExecuteAsync(async ctx =>
        {
            await Task.Yield();
            observedGen = ctx;
            ctx.SetProperty("added_in_gen", "val_in_gen");
            return 123;
        }, originalContext);

        res.Should().Be(123);
        observedGen.Should().NotBeNull();
        observedGen!.AttemptNumber.Should().Be(77);
        observedGen.Properties["replaced_key_gen"].Should().Be("replaced_val_gen");
        replacedContext.Properties["added_in_gen"].Should().Be("val_in_gen");
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
