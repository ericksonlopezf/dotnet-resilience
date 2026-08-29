// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using Xunit;

namespace EricksonLopez.Resilience.Polly.Tests;

public sealed class PollyPipelineBuilderTranslatorTests
{
    [Fact]
    public void TranslateAndBuild_WithNullBuilder_ThrowsArgumentNullException()
    {
        var act1 = () => PollyPipelineBuilderTranslator.TranslateAndBuild(null!);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => PollyPipelineBuilderTranslator.TranslateAndBuild<string>(null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task TranslateAndBuild_Timeout_WithOnTimeoutCallback_ExecutesCallback()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("timeout-cb-policy");
        var onTimeoutCalled = false;
        TimeSpan? observedTimeout = null;

        builder.AddTimeout(opt =>
        {
            opt.Timeout = TimeSpan.FromMilliseconds(20);
            opt.Name = "CustomTimeout";
            opt.OnTimeout = ctx =>
            {
                onTimeoutCalled = true;
                observedTimeout = ctx.Timeout;
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync<int>(async ct =>
            {
                await Task.Delay(200, ct);
                return 1;
            });
        };

        // Assert
        await act.Should().ThrowAsync<ResilienceTimeoutException>();
        onTimeoutCalled.Should().BeTrue();
        observedTimeout.Should().Be(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task TranslateAndBuild_TypedTimeout_WithOnTimeoutCallback_ExecutesCallback()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-timeout-cb-policy");
        var onTimeoutCalled = false;

        builder.AddTimeout(opt =>
        {
            opt.Timeout = TimeSpan.FromMilliseconds(20);
            opt.OnTimeout = ctx =>
            {
                onTimeoutCalled = true;
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(200, ct);
                return "res";
            });
        };

        // Assert
        await act.Should().ThrowAsync<ResilienceTimeoutException>();
        onTimeoutCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    [InlineData(BackoffType.Exponential)]
    [InlineData(BackoffType.ExponentialWithJitter)]
    public async Task TranslateAndBuild_Retry_WithDifferentBackoffsAndOnRetry_ExecutesCorrectly(BackoffType backoffType)
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("retry-backoff-" + backoffType);
        var retries = 0;
        var attempts = 0;

        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.MaxDelay = TimeSpan.FromSeconds(1);
            opt.BackoffType = backoffType;
            opt.Name = "CustomRetry";
            opt.ShouldHandleException = ex => ex is SocketException;
            opt.ShouldHandleResult = res => (res as string) == "RetryMe";
            opt.OnRetry = ctx =>
            {
                retries++;
                ctx.AttemptNumber.Should().Be(retries);
                ctx.ResilienceContext.AttemptNumber.Should().Be(retries);
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act - Exception retry
        var resultEx = await pipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new SocketException();
            }

            return ValueTask.FromResult("SuccessAfterEx");
        });

        // Assert
        resultEx.Should().Be("SuccessAfterEx");
        retries.Should().Be(1);
    }

    [Theory]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    [InlineData(BackoffType.Exponential)]
    [InlineData(BackoffType.ExponentialWithJitter)]
    public async Task TranslateAndBuild_TypedRetry_WithDifferentBackoffsAndOnRetry_ExecutesCorrectly(BackoffType backoffType)
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-retry-backoff-" + backoffType);
        var retries = 0;
        var attempts = 0;

        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.MaxDelay = TimeSpan.FromSeconds(1);
            opt.BackoffType = backoffType;
            opt.Name = "CustomTypedRetry";
            opt.ShouldHandleException = ex => ex is IOException;
            opt.ShouldHandleResult = res => (res as string) == "RetryThis";
            opt.OnRetry = ctx =>
            {
                retries++;
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts == 1)
            {
                return ValueTask.FromResult("RetryThis");
            }

            return ValueTask.FromResult("Done");
        });

        // Assert
        result.Should().Be("Done");
        retries.Should().Be(1);
    }

    [Fact]
    public async Task TranslateAndBuild_CircuitBreaker_AllStateCallbacks_AreInvoked()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("cb-callbacks");
        var openedCalled = false;
        var halfOpenedCalled = false;
        var closedCalled = false;

        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = ex => true;
            opt.ShouldHandleResult = res => (res as string) == "Bad";
            opt.OnCircuitOpened = ctx =>
            {
                openedCalled = true;
                ctx.State.Should().Be(CircuitBreakerState.Open);
                return ValueTask.CompletedTask;
            };
            opt.OnCircuitHalfOpened = ctx =>
            {
                halfOpenedCalled = true;
                ctx.State.Should().Be(CircuitBreakerState.HalfOpen);
                return ValueTask.CompletedTask;
            };
            opt.OnCircuitClosed = ctx =>
            {
                closedCalled = true;
                ctx.State.Should().Be(CircuitBreakerState.Closed);
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Fail 2 times to open
        for (var i = 0; i < 2; i++)
        {
            try
            {
                await pipeline.ExecuteAsync<string>(ct => throw new InvalidOperationException("boom"));
            }
            catch (InvalidOperationException) { }
        }

        openedCalled.Should().BeTrue();

        // Wait for break duration to transition to half-open
        await Task.Delay(600);

        // Successful execution closes circuit
        var successResult = await pipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("Recovered"));
        successResult.Should().Be("Recovered");

        halfOpenedCalled.Should().BeTrue();
        closedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TranslateAndBuild_TypedCircuitBreaker_AllStateCallbacks_AreInvoked()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-cb-callbacks");
        var openedCalled = false;
        var halfOpenedCalled = false;
        var closedCalled = false;

        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = ex => true;
            opt.ShouldHandleResult = res => (res as string) == "ErrorResult";
            opt.OnCircuitOpened = ctx =>
            {
                openedCalled = true;
                ctx.State.Should().Be(CircuitBreakerState.Open);
                return ValueTask.CompletedTask;
            };
            opt.OnCircuitHalfOpened = ctx =>
            {
                halfOpenedCalled = true;
                ctx.State.Should().Be(CircuitBreakerState.HalfOpen);
                return ValueTask.CompletedTask;
            };
            opt.OnCircuitClosed = ctx =>
            {
                closedCalled = true;
                ctx.State.Should().Be(CircuitBreakerState.Closed);
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        for (var i = 0; i < 2; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(ct => throw new InvalidOperationException("fail"));
            }
            catch (InvalidOperationException) { }
        }

        openedCalled.Should().BeTrue();

        await Task.Delay(600);

        var res = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Healthy"));
        res.Should().Be("Healthy");

        halfOpenedCalled.Should().BeTrue();
        closedCalled.Should().BeTrue();
    }

    [Theory]
    [InlineData(RateLimiterType.FixedWindow)]
    [InlineData(RateLimiterType.Concurrency)]
    [InlineData(RateLimiterType.TokenBucket)]
    [InlineData(RateLimiterType.SlidingWindow)]
    public async Task TranslateAndBuild_AllRateLimiterTypes_ConfigureAndExecute(RateLimiterType limiterType)
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("rl-type-" + limiterType);
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = limiterType;
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromSeconds(10);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("allowed"));

        // Assert
        result.Should().Be("allowed");
    }

    [Theory]
    [InlineData(RateLimiterType.FixedWindow)]
    [InlineData(RateLimiterType.Concurrency)]
    [InlineData(RateLimiterType.TokenBucket)]
    [InlineData(RateLimiterType.SlidingWindow)]
    public async Task TranslateAndBuild_TypedAllRateLimiterTypes_ConfigureAndExecute(RateLimiterType limiterType)
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<int>("typed-rl-type-" + limiterType);
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = limiterType;
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromSeconds(10);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var result = await pipeline.ExecuteAsync(ct => ValueTask.FromResult(77));

        // Assert
        result.Should().Be(77);
    }

    [Fact]
    public async Task TranslateAndBuild_TypedRateLimiter_WithCustomRateLimiter_UsesCustomInstance()
    {
        // Arrange
        var customLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            QueueLimit = 0,
            Window = TimeSpan.FromSeconds(30)
        });

        var builder = new ResiliencePipelineBuilder<string>("typed-custom-limiter");
        builder.AddRateLimiter(opt =>
        {
            opt.CustomRateLimiter = customLimiter;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act
        var res1 = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("ok"));
        res1.Should().Be("ok");

        Func<Task> act = async () => await pipeline.ExecuteAsync(ct => ValueTask.FromResult("fail"));
        await act.Should().ThrowAsync<RateLimitRejectedException>();
    }

    [Fact]
    public async Task TranslateAndBuild_Hedging_Untyped_UsesDefaultTransientClassifierWhenPredicateIsNull()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("untyped-hedge-default-classifier");
        builder.AddHedging(opt =>
        {
            opt.MaxHedgedAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.Name = null;
            opt.ShouldHandleException = null;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var attempts = 0;

        // Act - TimeoutException is transient
        var result = await pipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TimeoutException();
            }

            return ValueTask.FromResult("Recovered");
        });

        // Assert
        result.Should().Be("Recovered");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_Hedging_UntypedInTypedBuilder_ExecutesRetry()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-untyped-hedge-injected");
        var field = typeof(ResiliencePipelineBuilder<string>).GetField("_strategies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var strategies = (System.Collections.Generic.List<EricksonLopez.Resilience.Options.ResilienceStrategyOptions>)field.GetValue(builder)!;
        strategies.Add(new HedgingStrategyOptions
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = null,
            ShouldHandleException = null
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var attempts = 0;

        // Act
        var result = await pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new TimeoutException();
            }

            return ValueTask.FromResult("RecoveredTyped");
        });

        // Assert
        result.Should().Be("RecoveredTyped");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_TypedHedging_WhenMatchingFailureOccurs_SpawnsHedgedAttemptAndCallsCallback()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-hedging-parallel-callback");
        var onHedgingCalled = false;
        var attempts = 0;

        builder.AddHedging(new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(10),
            Name = null,
            ShouldHandleException = ex => ex is TimeoutException,
            ShouldHandleResult = res => (res as string) == "Slow",
            OnHedging = ctx =>
            {
                onHedgingCalled = true;
                ctx.AttemptNumber.Should().BeGreaterThanOrEqualTo(0);
                return ValueTask.CompletedTask;
            }
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // Act - 1st attempt throws TimeoutException which is handled, triggering hedge attempt
        var result = await pipeline.ExecuteAsync(async ct =>
        {
            var currentAttempt = Interlocked.Increment(ref attempts);
            if (currentAttempt == 1)
            {
                throw new TimeoutException("First attempt failed");
            }

            await Task.Yield();
            return "HedgeSuccess";
        });

        // Assert
        result.Should().Be("HedgeSuccess");
        onHedgingCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TranslateAndBuild_Retry_WithNullPredicate_UsesTransientClassifier()
    {
        // Untyped
        var untypedBuilder = new ResiliencePipelineBuilder("retry-null-pred");
        untypedBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = null;
        });

        var untypedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(untypedBuilder);
        var attempts = 0;
        var res = await untypedPipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            if (attempts == 1) throw new TimeoutException();
            return ValueTask.FromResult("done");
        });
        res.Should().Be("done");
        attempts.Should().Be(2);

        // Typed
        var typedBuilder = new ResiliencePipelineBuilder<string>("typed-retry-null-pred");
        typedBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = null;
        });

        var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
        var typedAttempts = 0;
        var typedRes = await typedPipeline.ExecuteAsync(ct =>
        {
            typedAttempts++;
            if (typedAttempts == 1) throw new TimeoutException();
            return ValueTask.FromResult("done-typed");
        });
        typedRes.Should().Be("done-typed");
        typedAttempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_CircuitBreaker_WithNullPredicate_UsesTransientClassifier()
    {
        // Untyped
        var builder = new ResiliencePipelineBuilder("cb-null-pred");
        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = null;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        for (var i = 0; i < 2; i++)
        {
            try { await pipeline.ExecuteAsync<string>(ct => throw new TimeoutException()); }
            catch (TimeoutException) { }
        }

        Func<Task> act = async () => await pipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("ok"));
        await act.Should().ThrowAsync<CircuitBrokenException>();

        // Typed
        var typedBuilder = new ResiliencePipelineBuilder<string>("typed-cb-null-pred");
        typedBuilder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = null;
        });

        var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
        for (var i = 0; i < 2; i++)
        {
            try { await typedPipeline.ExecuteAsync(ct => throw new TimeoutException()); }
            catch (TimeoutException) { }
        }

        Func<Task> actTyped = async () => await typedPipeline.ExecuteAsync(ct => ValueTask.FromResult("ok"));
        await actTyped.Should().ThrowAsync<CircuitBrokenException>();
    }

    [Fact]
    public async Task TranslateAndBuild_Fallback_WithNullPredicate_UsesTransientClassifier()
    {
        var builder = new ResiliencePipelineBuilder<string>("fallback-null-pred");
        builder.AddFallback(opt =>
        {
            opt.ShouldHandleException = null;
            opt.FallbackAction = ctx => ValueTask.FromResult("fallback-val");
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var res = await pipeline.ExecuteAsync(ct => throw new TimeoutException());
        res.Should().Be("fallback-val");
    }

    [Fact]
    public async Task TranslateAndBuild_Hedging_ExplicitPredicates_ConfiguresCorrectly()
    {
        // Non-generic AddHedging with explicit ShouldHandleException
        var nonGenericBuilder = new ResiliencePipelineBuilder("untyped-hedge-explicit");
        nonGenericBuilder.AddHedging(opt =>
        {
            opt.MaxHedgedAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.Name = "CustomHedge";
            opt.ShouldHandleException = ex => ex is InvalidOperationException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(nonGenericBuilder);
        var attempts = 0;
        var res = await pipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            if (attempts == 1) throw new InvalidOperationException();
            return ValueTask.FromResult("ok");
        });
        res.Should().Be("ok");
        attempts.Should().Be(2);

        // Typed AddHedgingFallbackTyped with explicit ShouldHandleException
        var typedBuilder = new ResiliencePipelineBuilder<string>("typed-hedge-explicit");
        var field = typeof(ResiliencePipelineBuilder<string>).GetField("_strategies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var strategies = (System.Collections.Generic.List<EricksonLopez.Resilience.Options.ResilienceStrategyOptions>)field.GetValue(typedBuilder)!;
        strategies.Add(new HedgingStrategyOptions
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = "ExplicitTypedHedge",
            ShouldHandleException = ex => ex is InvalidOperationException
        });

        var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
        var typedAttempts = 0;
        var typedRes = await typedPipeline.ExecuteAsync(ct =>
        {
            typedAttempts++;
            if (typedAttempts == 1) throw new InvalidOperationException();
            return ValueTask.FromResult("ok");
        });
        typedRes.Should().Be("ok");
        typedAttempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_TypedRetry_WhenPredicateReturnsFalse_DoesNotRetry()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-retry-no-match");
        var retries = 0;
        var attempts = 0;

        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is IOException;
            opt.OnRetry = ctx =>
            {
                retries++;
                return ValueTask.CompletedTask;
            };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        Func<Task> act = async () => await pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            throw new ArgumentException("Non transient");
        });

        await act.Should().ThrowAsync<ArgumentException>();
        attempts.Should().Be(1);
        retries.Should().Be(0);
    }

    [Fact]
    public async Task TranslateAndBuild_TypedHedging_WithNullPredicate_UsesTransientClassifier()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-hedging-null-pred");
        var onHedgingCalled = false;
        var attempts = 0;

        builder.AddHedging(new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(10),
            ShouldHandleException = null,
            OnHedging = ctx =>
            {
                onHedgingCalled = true;
                return ValueTask.CompletedTask;
            }
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        var result = await pipeline.ExecuteAsync(async ct =>
        {
            var cur = Interlocked.Increment(ref attempts);
            if (cur == 1) throw new TimeoutException();
            await Task.Yield();
            return "HedgeTransientSuccess";
        });

        result.Should().Be("HedgeTransientSuccess");
        onHedgingCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TranslateAndBuild_UntypedRetry_WithShouldHandleResult_RetriesOnResult()
    {
        var builder = new ResiliencePipelineBuilder("untyped-retry-result");
        var attempts = 0;
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleResult = res => (string?)res == "RetryThis";
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        var res = await pipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            return ValueTask.FromResult(attempts == 1 ? "RetryThis" : "FinalOk");
        });

        res.Should().Be("FinalOk");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_CircuitBreaker_WithShouldHandleResult_BreaksCircuitOnMatchingResult()
    {
        // Untyped
        var builder = new ResiliencePipelineBuilder("untyped-cb-result");
        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleResult = res => (string?)res == "FailResult";
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        for (var i = 0; i < 2; i++)
        {
            await pipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("FailResult"));
        }

        Func<Task> act = async () => await pipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("ok"));
        await act.Should().ThrowAsync<CircuitBrokenException>();

        // Typed
        var typedBuilder = new ResiliencePipelineBuilder<string>("typed-cb-result");
        typedBuilder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleResult = res => (string?)res == "TypedFailResult";
        });

        var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
        for (var i = 0; i < 2; i++)
        {
            await typedPipeline.ExecuteAsync(ct => ValueTask.FromResult("TypedFailResult"));
        }

        Func<Task> actTyped = async () => await typedPipeline.ExecuteAsync(ct => ValueTask.FromResult("ok"));
        await actTyped.Should().ThrowAsync<CircuitBrokenException>();
    }

    [Fact]
    public async Task TranslateAndBuild_Callbacks_WhenExecutedWithoutEcosystemContext_UsesFallbackContextWithCorrectName()
    {
        // 1. Untyped Timeout Callback with default and custom name
        string? capturedTimeoutPolicy = null;
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        var translatorType = typeof(PollyPipelineBuilderTranslator);
        var addTimeoutMethod = translatorType.GetMethod("AddTimeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var timeoutOpt = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            Name = null, // tests fallback name "Timeout"
            OnTimeout = ctx =>
            {
                capturedTimeoutPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addTimeoutMethod.Invoke(null, new object[] { pollyBuilder, timeoutOpt });
        var pollyPipeline = pollyBuilder.Build();

        try
        {
            await pollyPipeline.ExecuteAsync(async ct => { await Task.Delay(200, ct); return "late"; });
        }
        catch (global::Polly.Timeout.TimeoutRejectedException) { }

        capturedTimeoutPolicy.Should().Be("Timeout");

        // 2. Typed Timeout Callback with default and custom name
        string? capturedTypedTimeoutPolicy = null;
        var typedPollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var addTimeoutTypedMethod = translatorType.GetMethod("AddTimeoutTyped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(typeof(string));

        var defaultTypedTimeoutOpt = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            Name = null, // fallback "Timeout"
            OnTimeout = ctx =>
            {
                capturedTypedTimeoutPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addTimeoutTypedMethod.Invoke(null, new object[] { typedPollyBuilder, defaultTypedTimeoutOpt });
        var typedPollyPipeline = typedPollyBuilder.Build();

        try
        {
            await typedPollyPipeline.ExecuteAsync(async ct => { await Task.Delay(200, ct); return "late"; });
        }
        catch (global::Polly.Timeout.TimeoutRejectedException) { }

        capturedTypedTimeoutPolicy.Should().Be("Timeout");

        // 3. Untyped Retry Callback with default and custom name
        string? capturedRetryPolicy = null;
        var retryPollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        var addRetryMethod = translatorType.GetMethod("AddRetry", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var retryOpt = new RetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = null, // fallback "Retry"
            OnRetry = ctx =>
            {
                capturedRetryPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addRetryMethod.Invoke(null, new object[] { retryPollyBuilder, retryOpt });
        var retryPipeline = retryPollyBuilder.Build();
        var attempt = 0;
        await retryPipeline.ExecuteAsync(ct =>
        {
            attempt++;
            if (attempt == 1) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        });
        capturedRetryPolicy.Should().Be("Retry");

        // 4. Typed Retry Callback with default name & attempt increment
        string? capturedTypedRetryPolicy = null;
        var attemptObserved = 0;
        var contextAttemptObserved = 0;
        var typedRetryPollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var addRetryTypedMethod = translatorType.GetMethod("AddRetryTyped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(typeof(string));

        var typedRetryOpt = new RetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = null, // fallback "Retry"
            OnRetry = ctx =>
            {
                capturedTypedRetryPolicy = ctx.ResilienceContext.PolicyName;
                attemptObserved = ctx.AttemptNumber;
                contextAttemptObserved = ctx.ResilienceContext.AttemptNumber;
                return ValueTask.CompletedTask;
            }
        };
        addRetryTypedMethod.Invoke(null, new object[] { typedRetryPollyBuilder, typedRetryOpt });
        var typedRetryPipeline = typedRetryPollyBuilder.Build();
        var typedAttempt = 0;
        await typedRetryPipeline.ExecuteAsync(ct =>
        {
            typedAttempt++;
            if (typedAttempt == 1) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        });
        capturedTypedRetryPolicy.Should().Be("Retry");
        attemptObserved.Should().Be(1);
        contextAttemptObserved.Should().Be(1);

        // 5. Typed Fallback Callback with default name
        string? capturedFallbackPolicy = null;
        var fallbackPollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var addFallbackTypedMethod = translatorType.GetMethod("AddFallbackTyped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(typeof(string));

        var fallbackOpt = new FallbackStrategyOptions<string>
        {
            Name = null, // fallback "Fallback"
            FallbackAction = ctx => ValueTask.FromResult("fb"),
            OnFallback = ctx =>
            {
                capturedFallbackPolicy = ctx.Context.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addFallbackTypedMethod.Invoke(null, new object[] { fallbackPollyBuilder, fallbackOpt });
        var fallbackPipeline = fallbackPollyBuilder.Build();
        var fbRes = await fallbackPipeline.ExecuteAsync(static async ct =>
        {
            await Task.Yield();
            if (ct.CanBeCanceled || !ct.CanBeCanceled)
            {
                throw new TimeoutException();
            }
            return "ok";
        });
        fbRes.Should().Be("fb");
        capturedFallbackPolicy.Should().Be("Fallback");

        // 6. Typed Hedging Callback with default name
        string? capturedHedgingPolicy = null;
        var hedgePollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var addHedgingMethod = translatorType.GetMethod("AddHedgingParallel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(typeof(string));

        var hedgeOpt = new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = null, // fallback "Hedging"
            OnHedging = ctx =>
            {
                capturedHedgingPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addHedgingMethod.Invoke(null, new object[] { hedgePollyBuilder, hedgeOpt });
        var hedgePipeline = hedgePollyBuilder.Build();
        var hedgeAttempt = 0;
        var hRes = await hedgePipeline.ExecuteAsync(async ct =>
        {
            var cur = Interlocked.Increment(ref hedgeAttempt);
            if (cur == 1) throw new TimeoutException();
            await Task.Yield();
            return "hOk";
        });
        hRes.Should().Be("hOk");
        capturedHedgingPolicy.Should().Be("Hedging");

        // 7. Custom Names without Ecosystem Context (kills null coalescing remove left mutants)
        // Timeout Custom
        string? customTimeoutPolicy = null;
        var customTimeoutBuilder = new global::Polly.ResiliencePipelineBuilder();
        var customTimeoutOpt = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            Name = "CustomTimeoutPolicyName",
            OnTimeout = ctx =>
            {
                customTimeoutPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addTimeoutMethod.Invoke(null, new object[] { customTimeoutBuilder, customTimeoutOpt });
        try
        {
            await customTimeoutBuilder.Build().ExecuteAsync(async ct =>
            {
                await Task.Delay(200, ct);
                return "late";
            });
        }
        catch (global::Polly.Timeout.TimeoutRejectedException) { }
        customTimeoutPolicy.Should().Be("CustomTimeoutPolicyName");

        // Typed Timeout Custom
        string? customTypedTimeoutPolicy = null;
        var customTypedTimeoutBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var customTypedTimeoutOpt = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromMilliseconds(20),
            Name = "CustomTypedTimeoutPolicyName",
            OnTimeout = ctx =>
            {
                customTypedTimeoutPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addTimeoutTypedMethod.Invoke(null, new object[] { customTypedTimeoutBuilder, customTypedTimeoutOpt });
        try
        {
            await customTypedTimeoutBuilder.Build().ExecuteAsync(async ct =>
            {
                await Task.Delay(200, ct);
                return "late";
            });
        }
        catch (global::Polly.Timeout.TimeoutRejectedException) { }
        customTypedTimeoutPolicy.Should().Be("CustomTypedTimeoutPolicyName");

        // Retry Custom
        string? customRetryPolicy = null;
        var customRetryBuilder = new global::Polly.ResiliencePipelineBuilder();
        var customRetryOpt = new RetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = "CustomRetryPolicyName",
            OnRetry = ctx =>
            {
                customRetryPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addRetryMethod.Invoke(null, new object[] { customRetryBuilder, customRetryOpt });
        var cAttempts = 0;
        await customRetryBuilder.Build().ExecuteAsync(ct =>
        {
            cAttempts++;
            if (cAttempts == 1) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        });
        customRetryPolicy.Should().Be("CustomRetryPolicyName");

        // Typed Retry Custom
        string? customTypedRetryPolicy = null;
        var customTypedRetryBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var customTypedRetryOpt = new RetryStrategyOptions
        {
            MaxRetryAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = "CustomTypedRetryPolicyName",
            OnRetry = ctx =>
            {
                customTypedRetryPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addRetryTypedMethod.Invoke(null, new object[] { customTypedRetryBuilder, customTypedRetryOpt });
        var ctAttempts = 0;
        await customTypedRetryBuilder.Build().ExecuteAsync(ct =>
        {
            ctAttempts++;
            if (ctAttempts == 1) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        });
        customTypedRetryPolicy.Should().Be("CustomTypedRetryPolicyName");

        // Fallback Custom
        string? customFallbackPolicy = null;
        var customFallbackBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var customFallbackOpt = new FallbackStrategyOptions<string>
        {
            Name = "CustomFallbackPolicyName",
            FallbackAction = ctx => ValueTask.FromResult("customFb"),
            OnFallback = ctx =>
            {
                customFallbackPolicy = ctx.Context.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addFallbackTypedMethod.Invoke(null, new object[] { customFallbackBuilder, customFallbackOpt });
        var cFbRes = await customFallbackBuilder.Build().ExecuteAsync(static async ct =>
        {
            await Task.Yield();
            if (ct.CanBeCanceled || !ct.CanBeCanceled)
            {
                throw new TimeoutException();
            }
            return "ok";
        });
        cFbRes.Should().Be("customFb");
        customFallbackPolicy.Should().Be("CustomFallbackPolicyName");

        // Hedging Custom
        string? customHedgingPolicy = null;
        var customHedgeBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var customHedgeOpt = new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = "CustomHedgingPolicyName",
            OnHedging = ctx =>
            {
                customHedgingPolicy = ctx.ResilienceContext.PolicyName;
                return ValueTask.CompletedTask;
            }
        };
        addHedgingMethod.Invoke(null, new object[] { customHedgeBuilder, customHedgeOpt });
        var cHedgeAttempt = 0;
        var cHRes = await customHedgeBuilder.Build().ExecuteAsync(async ct =>
        {
            var cur = Interlocked.Increment(ref cHedgeAttempt);
            if (cur == 1) throw new TimeoutException();
            await Task.Yield();
            return "cHedgeOk";
        });
        cHRes.Should().Be("cHedgeOk");
        customHedgingPolicy.Should().Be("CustomHedgingPolicyName");
    }

    [Theory]
    [InlineData(BackoffType.Constant)]
    [InlineData(BackoffType.Linear)]
    [InlineData(BackoffType.Exponential)]
    [InlineData(BackoffType.ExponentialWithJitter)]
    public async Task TranslateAndBuild_AllBackoffTypes_ConfiguresCorrectly(BackoffType backoffType)
    {
        // Untyped
        var untypedBuilder = new ResiliencePipelineBuilder($"untyped-backoff-{backoffType}");
        TimeSpan? untypedRetryDelay = null;
        untypedBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(50);
            opt.BackoffType = backoffType;
            opt.OnRetry = ctx =>
            {
                untypedRetryDelay = ctx.Delay;
                return ValueTask.CompletedTask;
            };
        });
        var untypedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(untypedBuilder);
        var attempts = 0;
        var res = await untypedPipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            if (attempts < 2) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        });
        res.Should().Be("ok");
        attempts.Should().Be(2);
        if (backoffType == BackoffType.Exponential)
        {
            untypedRetryDelay.Should().Be(TimeSpan.FromMilliseconds(50));
        }

        // Typed
        var typedBuilder = new ResiliencePipelineBuilder<string>($"typed-backoff-{backoffType}");
        TimeSpan? typedRetryDelay = null;
        typedBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(50);
            opt.BackoffType = backoffType;
            opt.OnRetry = ctx =>
            {
                typedRetryDelay = ctx.Delay;
                return ValueTask.CompletedTask;
            };
        });
        var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
        var typedAttempts = 0;
        var typedRes = await typedPipeline.ExecuteAsync(ct =>
        {
            typedAttempts++;
            if (typedAttempts < 2) throw new TimeoutException();
            return ValueTask.FromResult("ok-typed");
        });
        typedRes.Should().Be("ok-typed");
        typedAttempts.Should().Be(2);
        if (backoffType == BackoffType.Exponential)
        {
            typedRetryDelay.Should().Be(TimeSpan.FromMilliseconds(50));
        }
    }

    [Fact]
    public async Task TranslateAndBuild_HedgingFallback_UntypedAndTyped_ConfiguresPropertiesCorrectly()
    {
        // Untyped with default name and transient predicate
        var untypedBuilder = new ResiliencePipelineBuilder("untyped-hedge-fb-default");
        untypedBuilder.AddHedging(opt =>
        {
            opt.MaxHedgedAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.Name = null; // fallback "Hedging"
            opt.ShouldHandleException = null; // transient classifier
        });
        var untypedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(untypedBuilder);
        var attempts = 0;
        var res = await untypedPipeline.ExecuteAsync<string>(ct =>
        {
            attempts++;
            if (attempts < 3) throw new TimeoutException();
            return ValueTask.FromResult("untyped-done");
        });
        res.Should().Be("untyped-done");
        attempts.Should().Be(3);

        // Untyped with non-matching exception (does not retry)
        var untypedNoMatch = new ResiliencePipelineBuilder("untyped-hedge-fb-nomatch");
        untypedNoMatch.AddHedging(opt =>
        {
            opt.MaxHedgedAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is IOException;
        });
        var untypedNoMatchPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(untypedNoMatch);
        var nomatchAttempts = 0;
        Func<Task> actNomatch = async () => await untypedNoMatchPipeline.ExecuteAsync<string>(ct =>
        {
            nomatchAttempts++;
            throw new ArgumentException("No match");
        });
        await actNomatch.Should().ThrowAsync<ArgumentException>();
        nomatchAttempts.Should().Be(1);

        // Typed with custom name and explicit predicate
        var typedBuilder = new ResiliencePipelineBuilder<string>("typed-hedge-fb-custom");
        var field = typeof(ResiliencePipelineBuilder<string>).GetField("_strategies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var strategies = (System.Collections.Generic.List<EricksonLopez.Resilience.Options.ResilienceStrategyOptions>)field.GetValue(typedBuilder)!;
        strategies.Add(new HedgingStrategyOptions
        {
            MaxHedgedAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = "CustomTypedHedging",
            ShouldHandleException = ex => ex is InvalidOperationException
        });
        var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
        var typedAttempts = 0;
        var typedRes = await typedPipeline.ExecuteAsync(ct =>
        {
            typedAttempts++;
            if (typedAttempts < 3) throw new InvalidOperationException();
            return ValueTask.FromResult("typed-done");
        });
        typedRes.Should().Be("typed-done");
        typedAttempts.Should().Be(3);

        // Typed with default name
        var typedDefaultBuilder = new ResiliencePipelineBuilder<string>("typed-hedge-fb-default");
        var defaultStrategies = (System.Collections.Generic.List<EricksonLopez.Resilience.Options.ResilienceStrategyOptions>)field.GetValue(typedDefaultBuilder)!;
        defaultStrategies.Add(new HedgingStrategyOptions
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(5),
            Name = null, // fallback "Hedging"
            ShouldHandleException = null // transient classifier
        });
        var typedDefaultPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedDefaultBuilder);
        var defAttempts = 0;
        var defRes = await typedDefaultPipeline.ExecuteAsync(ct =>
        {
            defAttempts++;
            if (defAttempts < 2) throw new TimeoutException();
            return ValueTask.FromResult("typed-def-done");
        });
        defRes.Should().Be("typed-def-done");
        defAttempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_Callbacks_PreservesEcosystemContextProperties()
    {
        var inputContext = ResilienceContext.Create("test-preserve")
            .WithCorrelationId("corr-12345")
            .WithTenantId("tenant-99999");

        // 1. Untyped Timeout Callback
        var timeoutBuilder = new ResiliencePipelineBuilder("timeout-preserve");
        string? timeoutCorr = null;
        string? timeoutTenant = null;
        timeoutBuilder.AddTimeout(opt =>
        {
            opt.Timeout = TimeSpan.FromMilliseconds(20);
            opt.OnTimeout = ctx =>
            {
                timeoutCorr = ctx.ResilienceContext.CorrelationId;
                timeoutTenant = ctx.ResilienceContext.TenantId;
                return ValueTask.CompletedTask;
            };
        });
        var timeoutPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(timeoutBuilder);
        try
        {
            await timeoutPipeline.ExecuteAsync<string>(async ctx =>
            {
                await Task.Delay(200, ctx.CancellationToken);
                return "ok";
            }, inputContext);
        }
        catch (ResilienceTimeoutException) { }
        timeoutCorr.Should().Be("corr-12345");
        timeoutTenant.Should().Be("tenant-99999");

        // 2. Typed Timeout Callback
        var typedTimeoutBuilder = new ResiliencePipelineBuilder<string>("typed-timeout-preserve");
        string? typedTimeoutCorr = null;
        typedTimeoutBuilder.AddTimeout(opt =>
        {
            opt.Timeout = TimeSpan.FromMilliseconds(20);
            opt.OnTimeout = ctx =>
            {
                typedTimeoutCorr = ctx.ResilienceContext.CorrelationId;
                return ValueTask.CompletedTask;
            };
        });
        var typedTimeoutPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedTimeoutBuilder);
        try
        {
            await typedTimeoutPipeline.ExecuteAsync(async ctx =>
            {
                await Task.Delay(200, ctx.CancellationToken);
                return "ok";
            }, inputContext);
        }
        catch (ResilienceTimeoutException) { }
        typedTimeoutCorr.Should().Be("corr-12345");

        // 3. Untyped Retry Callback
        var retryBuilder = new ResiliencePipelineBuilder("retry-preserve");
        string? retryCorr = null;
        retryBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 1;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.OnRetry = ctx =>
            {
                retryCorr = ctx.ResilienceContext.CorrelationId;
                return ValueTask.CompletedTask;
            };
        });
        var retryPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(retryBuilder);
        var rAttempts = 0;
        await retryPipeline.ExecuteAsync<string>(ctx =>
        {
            rAttempts++;
            if (rAttempts == 1) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        }, inputContext);
        retryCorr.Should().Be("corr-12345");

        // 4. Typed Retry Callback
        var typedRetryBuilder = new ResiliencePipelineBuilder<string>("typed-retry-preserve");
        string? typedRetryCorr = null;
        typedRetryBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 1;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.OnRetry = ctx =>
            {
                typedRetryCorr = ctx.ResilienceContext.CorrelationId;
                return ValueTask.CompletedTask;
            };
        });
        var typedRetryPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedRetryBuilder);
        var trAttempts = 0;
        await typedRetryPipeline.ExecuteAsync(ctx =>
        {
            trAttempts++;
            if (trAttempts == 1) throw new TimeoutException();
            return ValueTask.FromResult("ok");
        }, inputContext);
        typedRetryCorr.Should().Be("corr-12345");

        // 5. Typed Fallback Callback
        var fallbackBuilder = new ResiliencePipelineBuilder<string>("fallback-preserve");
        string? fallbackCorr = null;
        fallbackBuilder.AddFallback(opt =>
        {
            opt.FallbackAction = ctx => ValueTask.FromResult("fb");
            opt.OnFallback = ctx =>
            {
                fallbackCorr = ctx.Context.CorrelationId;
                return ValueTask.CompletedTask;
            };
        });
        var fallbackPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(fallbackBuilder);
        await fallbackPipeline.ExecuteAsync(ctx => throw new TimeoutException(), inputContext);
        fallbackCorr.Should().Be("corr-12345");

        // 6. Typed Hedging Callback
        var hedgeBuilder = new ResiliencePipelineBuilder<string>("hedge-preserve");
        string? hedgeCorr = null;
        hedgeBuilder.AddHedging(new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(5),
            OnHedging = ctx =>
            {
                hedgeCorr = ctx.ResilienceContext.CorrelationId;
                return ValueTask.CompletedTask;
            }
        });
        var hedgePipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(hedgeBuilder);
        var hAttempts = 0;
        await hedgePipeline.ExecuteAsync(async ctx =>
        {
            var cur = Interlocked.Increment(ref hAttempts);
            if (cur == 1) throw new TimeoutException();
            await Task.Yield();
            return "hOk";
        }, inputContext);
        hedgeCorr.Should().Be("corr-12345");
    }

    [Fact]
    public async Task TranslateAndBuild_ExplicitPredicate_WithCustomNonTransientException_RetriesAndFallsBackAndHedges()
    {
        // 1. Untyped Retry with non-transient exception
        var untypedRetryBuilder = new ResiliencePipelineBuilder("untyped-custom-ex");
        untypedRetryBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is CustomNonTransientException;
        });
        var untypedRetryPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(untypedRetryBuilder);
        var uAttempts = 0;
        var uRes = await untypedRetryPipeline.ExecuteAsync<string>(ct =>
        {
            uAttempts++;
            if (uAttempts == 1) throw new CustomNonTransientException();
            return ValueTask.FromResult("uOk");
        });
        uRes.Should().Be("uOk");
        uAttempts.Should().Be(2);

        // 2. Typed Retry with non-transient exception
        var typedRetryBuilder = new ResiliencePipelineBuilder<string>("typed-custom-ex");
        typedRetryBuilder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is CustomNonTransientException;
        });
        var typedRetryPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedRetryBuilder);
        var tAttempts = 0;
        var tRes = await typedRetryPipeline.ExecuteAsync(ct =>
        {
            tAttempts++;
            if (tAttempts == 1) throw new CustomNonTransientException();
            return ValueTask.FromResult("tOk");
        });
        tRes.Should().Be("tOk");
        tAttempts.Should().Be(2);

        // 3. Typed Fallback with non-transient exception
        var fallbackBuilder = new ResiliencePipelineBuilder<string>("fallback-custom-ex");
        fallbackBuilder.AddFallback(opt =>
        {
            opt.ShouldHandleException = ex => ex is CustomNonTransientException;
            opt.FallbackAction = ctx => ValueTask.FromResult("fbOk");
        });
        var fallbackPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(fallbackBuilder);
        var fbRes = await fallbackPipeline.ExecuteAsync(ct => throw new CustomNonTransientException());
        fbRes.Should().Be("fbOk");

        // 4. Typed Hedging with non-transient exception
        var hedgeBuilder = new ResiliencePipelineBuilder<string>("hedge-custom-ex");
        hedgeBuilder.AddHedging(new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(50),
            ShouldHandleException = ex => ex is CustomNonTransientException
        });
        var hedgePipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(hedgeBuilder);
        var hAttempts = 0;
        var hRes = await hedgePipeline.ExecuteAsync(async ct =>
        {
            var cur = Interlocked.Increment(ref hAttempts);
            if (cur == 1) throw new CustomNonTransientException();
            await Task.Yield();
            return "hOk";
        });
        hRes.Should().Be("hOk");
        hAttempts.Should().Be(2);

        // 5. Typed Hedging with ShouldHandleResult
        var hedgeResultBuilder = new ResiliencePipelineBuilder<string>("hedge-result-ex");
        hedgeResultBuilder.AddHedging(new HedgingStrategyOptions<string>
        {
            MaxHedgedAttempts = 1,
            Delay = TimeSpan.FromMilliseconds(50),
            ShouldHandleResult = res => res == "SlowResult"
        });
        var hedgeResultPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(hedgeResultBuilder);
        var hrAttempts = 0;
        var hrRes = await hedgeResultPipeline.ExecuteAsync(async ct =>
        {
            var cur = Interlocked.Increment(ref hrAttempts);
            if (cur == 1)
            {
                await Task.Yield();
                return "SlowResult";
            }
            return "FastResult";
        });
        hrRes.Should().Be("FastResult");
        hrAttempts.Should().Be(2);
    }

    [Fact]
    public async Task TranslateAndBuild_HedgingFallback_DirectInternalInvocation_VerifiesNoJitterAndName()
    {
        var translatorType = typeof(PollyPipelineBuilderTranslator);
        var addHedgeFbMethod = translatorType.GetMethod("AddHedgingFallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var addHedgeFbTypedMethod = translatorType.GetMethod("AddHedgingFallbackTyped", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(typeof(string));

        // 1. Untyped with Custom Name and constant delay
        var untypedPollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        var untypedHedgeOpt = new HedgingStrategyOptions
        {
            MaxHedgedAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(40),
            Name = "CustomHedgeFallbackUntyped"
        };
        addHedgeFbMethod.Invoke(null, new object[] { untypedPollyBuilder, untypedHedgeOpt });
        var untypedPipeline = untypedPollyBuilder.Build();
        var uAttempts = 0;
        var uRes = await untypedPipeline.ExecuteAsync<string>(ct =>
        {
            uAttempts++;
            if (uAttempts < 3) throw new TimeoutException();
            return ValueTask.FromResult("uDone");
        });
        uRes.Should().Be("uDone");
        uAttempts.Should().Be(3);

        // Inspect Untyped RetryStrategyOptions via reflection
        var untypedRetryOpt = GetFirstRetryOptions(untypedPollyBuilder);
        untypedRetryOpt.UseJitter.Should().BeFalse();
        untypedRetryOpt.Name.Should().Be("CustomHedgeFallbackUntyped");
        untypedRetryOpt.BackoffType.Should().Be(global::Polly.DelayBackoffType.Constant);
        untypedRetryOpt.Delay.Should().Be(TimeSpan.FromMilliseconds(40));
        untypedRetryOpt.MaxRetryAttempts.Should().Be(3);

        // 2. Typed with Custom Name and constant delay
        var typedPollyBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        var typedHedgeOpt = new HedgingStrategyOptions
        {
            MaxHedgedAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(40),
            Name = "CustomHedgeFallbackTyped"
        };
        addHedgeFbTypedMethod.Invoke(null, new object[] { typedPollyBuilder, typedHedgeOpt });
        var typedPipeline = typedPollyBuilder.Build();
        var tAttempts = 0;
        var tRes = await typedPipeline.ExecuteAsync(ct =>
        {
            tAttempts++;
            if (tAttempts < 3) throw new TimeoutException();
            return ValueTask.FromResult("tDone");
        });
        tRes.Should().Be("tDone");
        tAttempts.Should().Be(3);

        // Inspect Typed RetryStrategyOptions via reflection
        var typedRetryOpt = GetFirstRetryOptions(typedPollyBuilder);
        typedRetryOpt.UseJitter.Should().BeFalse();
        typedRetryOpt.Name.Should().Be("CustomHedgeFallbackTyped");
        typedRetryOpt.BackoffType.Should().Be(global::Polly.DelayBackoffType.Constant);
        typedRetryOpt.Delay.Should().Be(TimeSpan.FromMilliseconds(40));
        typedRetryOpt.MaxRetryAttempts.Should().Be(3);

        // 3. Test with default Name = null
        var defUntypedBuilder = new global::Polly.ResiliencePipelineBuilder();
        addHedgeFbMethod.Invoke(null, new object[] { defUntypedBuilder, new HedgingStrategyOptions { Name = null } });
        var defUntypedOpt = GetFirstRetryOptions(defUntypedBuilder);
        defUntypedOpt.Name.Should().Be("Hedging");

        var defTypedBuilder = new global::Polly.ResiliencePipelineBuilder<string>();
        addHedgeFbTypedMethod.Invoke(null, new object[] { defTypedBuilder, new HedgingStrategyOptions { Name = null } });
        var defTypedOpt = GetFirstRetryOptions(defTypedBuilder);
        defTypedOpt.Name.Should().Be("Hedging");
    }

    private static global::Polly.Retry.RetryStrategyOptions GetFirstRetryOptions(global::Polly.ResiliencePipelineBuilder builder)
    {
        var baseType = builder.GetType().BaseType!;
        var entriesField = baseType.GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var entries = (System.Collections.IList)entriesField.GetValue(builder)!;
        var entry = entries[0]!;
        var optionsProp = entry.GetType().GetProperty("Options", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (global::Polly.Retry.RetryStrategyOptions)optionsProp.GetValue(entry)!;
    }

    private static global::Polly.Retry.RetryStrategyOptions<T> GetFirstRetryOptions<T>(global::Polly.ResiliencePipelineBuilder<T> builder)
    {
        var baseType = builder.GetType().BaseType!;
        var entriesField = baseType.GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var entries = (System.Collections.IList)entriesField.GetValue(builder)!;
        var entry = entries[0]!;
        var optionsProp = entry.GetType().GetProperty("Options", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        return (global::Polly.Retry.RetryStrategyOptions<T>)optionsProp.GetValue(entry)!;
    }

    private sealed class CustomNonTransientException : Exception
    {
        public CustomNonTransientException() : base("Custom non-transient exception") { }
    }
}
