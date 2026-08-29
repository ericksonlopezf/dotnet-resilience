// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Options;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class ResilienceOptionsTests
{
    [Fact]
    public void RetryStrategyOptions_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new RetryStrategyOptions();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.MaxRetryAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromSeconds(2));
        options.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        options.MaxDelay.Should().BeNull();
        options.ShouldHandleException.Should().BeNull();
        options.ShouldHandleResult.Should().BeNull();
        options.OnRetry.Should().BeNull();
    }

    [Fact]
    public void RetryStrategyOptions_CanSetCustomValuesAndCallbacks()
    {
        // Arrange
        Func<Exception, bool> exPredicate = ex => ex is InvalidOperationException;
        Func<object?, bool> resPredicate = res => res == null;
        Func<RetryAttemptContext, ValueTask> onRetry = _ => ValueTask.CompletedTask;

        // Act
        var options = new RetryStrategyOptions
        {
            Name = "CustomRetry",
            Order = 5,
            MaxRetryAttempts = 10,
            Delay = TimeSpan.FromMilliseconds(100),
            BackoffType = BackoffType.Linear,
            MaxDelay = TimeSpan.FromSeconds(10),
            ShouldHandleException = exPredicate,
            ShouldHandleResult = resPredicate,
            OnRetry = onRetry
        };

        // Assert
        options.Name.Should().Be("CustomRetry");
        options.Order.Should().Be(5);
        options.MaxRetryAttempts.Should().Be(10);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(100));
        options.BackoffType.Should().Be(BackoffType.Linear);
        options.MaxDelay.Should().Be(TimeSpan.FromSeconds(10));
        options.ShouldHandleException.Should().BeSameAs(exPredicate);
        options.ShouldHandleResult.Should().BeSameAs(resPredicate);
        options.OnRetry.Should().BeSameAs(onRetry);
    }

    [Fact]
    public void CircuitBreakerStrategyOptions_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new CircuitBreakerStrategyOptions();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.FailureRatio.Should().Be(0.5);
        options.MinimumThroughput.Should().Be(20);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(10));
        options.ShouldHandleException.Should().BeNull();
        options.ShouldHandleResult.Should().BeNull();
        options.OnCircuitOpened.Should().BeNull();
        options.OnCircuitClosed.Should().BeNull();
        options.OnCircuitHalfOpened.Should().BeNull();
    }

    [Fact]
    public void CircuitBreakerStrategyOptions_CanSetCustomValuesAndCallbacks()
    {
        // Arrange
        Func<Exception, bool> exPred = _ => true;
        Func<object?, bool> resPred = _ => false;
        Func<CircuitBreakerStateContext, ValueTask> onOpen = _ => ValueTask.CompletedTask;
        Func<CircuitBreakerStateContext, ValueTask> onClose = _ => ValueTask.CompletedTask;
        Func<CircuitBreakerStateContext, ValueTask> onHalfOpen = _ => ValueTask.CompletedTask;

        // Act
        var options = new CircuitBreakerStrategyOptions
        {
            Name = "CB",
            Order = 2,
            FailureRatio = 0.8,
            MinimumThroughput = 50,
            SamplingDuration = TimeSpan.FromSeconds(60),
            BreakDuration = TimeSpan.FromSeconds(25),
            ShouldHandleException = exPred,
            ShouldHandleResult = resPred,
            OnCircuitOpened = onOpen,
            OnCircuitClosed = onClose,
            OnCircuitHalfOpened = onHalfOpen
        };

        // Assert
        options.Name.Should().Be("CB");
        options.Order.Should().Be(2);
        options.FailureRatio.Should().Be(0.8);
        options.MinimumThroughput.Should().Be(50);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(60));
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(25));
        options.ShouldHandleException.Should().BeSameAs(exPred);
        options.ShouldHandleResult.Should().BeSameAs(resPred);
        options.OnCircuitOpened.Should().BeSameAs(onOpen);
        options.OnCircuitClosed.Should().BeSameAs(onClose);
        options.OnCircuitHalfOpened.Should().BeSameAs(onHalfOpen);
    }

    [Fact]
    public void TimeoutStrategyOptions_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new TimeoutStrategyOptions();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.OnTimeout.Should().BeNull();
    }

    [Fact]
    public void TimeoutStrategyOptions_CanSetCustomValuesAndCallbacks()
    {
        // Arrange
        Func<TimeoutContext, ValueTask> onTimeout = _ => ValueTask.CompletedTask;

        // Act
        var options = new TimeoutStrategyOptions
        {
            Name = "TimeoutStrategy",
            Order = 1,
            Timeout = TimeSpan.FromSeconds(5),
            OnTimeout = onTimeout
        };

        // Assert
        options.Name.Should().Be("TimeoutStrategy");
        options.Order.Should().Be(1);
        options.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        options.OnTimeout.Should().BeSameAs(onTimeout);
    }

    [Fact]
    public void RateLimiterStrategyOptions_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new RateLimiterStrategyOptions();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.PermitLimit.Should().Be(100);
        options.QueueLimit.Should().Be(0);
        options.Window.Should().Be(TimeSpan.FromMinutes(1));
        options.LimiterType.Should().Be(RateLimiterType.SlidingWindow);
        options.CustomRateLimiter.Should().BeNull();
        options.OnRejected.Should().BeNull();
    }

    [Fact]
    public void RateLimiterStrategyOptions_CanSetCustomValuesAndCallbacks()
    {
        // Arrange
        Func<RateLimiterContext, ValueTask> onRejected = _ => ValueTask.CompletedTask;

        // Act
        var options = new RateLimiterStrategyOptions
        {
            Name = "RL",
            Order = 3,
            PermitLimit = 500,
            QueueLimit = 10,
            Window = TimeSpan.FromSeconds(15),
            LimiterType = RateLimiterType.Concurrency,
            OnRejected = onRejected
        };

        // Assert
        options.Name.Should().Be("RL");
        options.Order.Should().Be(3);
        options.PermitLimit.Should().Be(500);
        options.QueueLimit.Should().Be(10);
        options.Window.Should().Be(TimeSpan.FromSeconds(15));
        options.LimiterType.Should().Be(RateLimiterType.Concurrency);
        options.OnRejected.Should().BeSameAs(onRejected);
    }

    [Fact]
    public void HedgingStrategyOptions_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new HedgingStrategyOptions();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.MaxHedgedAttempts.Should().Be(2);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.ShouldHandleException.Should().BeNull();
        options.ShouldHandleResult.Should().BeNull();
    }

    [Fact]
    public void HedgingStrategyOptions_CanSetCustomValues()
    {
        // Arrange
        Func<Exception, bool> exPred = _ => true;
        Func<object?, bool> resPred = _ => true;

        // Act
        var options = new HedgingStrategyOptions
        {
            Name = "HedgeUntyped",
            Order = 4,
            MaxHedgedAttempts = 5,
            Delay = TimeSpan.FromMilliseconds(200),
            ShouldHandleException = exPred,
            ShouldHandleResult = resPred
        };

        // Assert
        options.Name.Should().Be("HedgeUntyped");
        options.Order.Should().Be(4);
        options.MaxHedgedAttempts.Should().Be(5);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(200));
        options.ShouldHandleException.Should().BeSameAs(exPred);
        options.ShouldHandleResult.Should().BeSameAs(resPred);
    }

    [Fact]
    public void HedgingStrategyOptionsT_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new HedgingStrategyOptions<string>();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.MaxHedgedAttempts.Should().Be(2);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.ShouldHandleException.Should().BeNull();
        options.ShouldHandleResult.Should().BeNull();
        options.OnHedging.Should().BeNull();
        options.HedgedActionGenerator.Should().BeNull();
    }

    [Fact]
    public void HedgingStrategyOptionsT_CanSetCustomValuesAndCallbacks()
    {
        // Arrange
        Func<Exception, bool> exPred = _ => false;
        Func<string, bool> resPred = res => string.IsNullOrEmpty(res);
        Func<HedgingContext, ValueTask> onHedging = _ => ValueTask.CompletedTask;
        Func<HedgingContext, Func<ValueTask<string>>?> generator = _ => () => ValueTask.FromResult("fallback");

        // Act
        var options = new HedgingStrategyOptions<string>
        {
            Name = "HedgeTyped",
            Order = 6,
            MaxHedgedAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(300),
            ShouldHandleException = exPred,
            ShouldHandleResult = resPred,
            OnHedging = onHedging,
            HedgedActionGenerator = generator
        };

        // Assert
        options.Name.Should().Be("HedgeTyped");
        options.Order.Should().Be(6);
        options.MaxHedgedAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(300));
        options.ShouldHandleException.Should().BeSameAs(exPred);
        options.ShouldHandleResult.Should().BeSameAs(resPred);
        options.OnHedging.Should().BeSameAs(onHedging);
        options.HedgedActionGenerator.Should().BeSameAs(generator);
    }

    [Fact]
    public void FallbackStrategyOptionsT_DefaultValues_AreStandard()
    {
        // Arrange & Act
        var options = new FallbackStrategyOptions<int>();

        // Assert
        options.Name.Should().BeNull();
        options.Order.Should().Be(0);
        options.FallbackAction.Should().BeNull();
        options.ShouldHandleException.Should().BeNull();
        options.ShouldHandleResult.Should().BeNull();
        options.OnFallback.Should().BeNull();
    }

    [Fact]
    public void FallbackStrategyOptionsT_CanSetCustomValuesAndCallbacks()
    {
        // Arrange
        Func<FallbackContext, ValueTask<int>> fallbackAction = _ => ValueTask.FromResult(-1);
        Func<Exception, bool> exPred = _ => true;
        Func<int, bool> resPred = r => r == 0;
        Func<FallbackContext, ValueTask> onFallback = _ => ValueTask.CompletedTask;

        // Act
        var options = new FallbackStrategyOptions<int>
        {
            Name = "FallbackInt",
            Order = 7,
            FallbackAction = fallbackAction,
            ShouldHandleException = exPred,
            ShouldHandleResult = resPred,
            OnFallback = onFallback
        };

        // Assert
        options.Name.Should().Be("FallbackInt");
        options.Order.Should().Be(7);
        options.FallbackAction.Should().BeSameAs(fallbackAction);
        options.ShouldHandleException.Should().BeSameAs(exPred);
        options.ShouldHandleResult.Should().BeSameAs(resPred);
        options.OnFallback.Should().BeSameAs(onFallback);
    }

    [Fact]
    public void Enums_HaveExpectedUnderlyingValues()
    {
        ((byte)BackoffType.Constant).Should().Be(0);
        ((byte)BackoffType.Linear).Should().Be(1);
        ((byte)BackoffType.Exponential).Should().Be(2);
        ((byte)BackoffType.ExponentialWithJitter).Should().Be(3);

        ((byte)CircuitBreakerState.Closed).Should().Be(0);
        ((byte)CircuitBreakerState.Open).Should().Be(1);
        ((byte)CircuitBreakerState.HalfOpen).Should().Be(2);
        ((byte)CircuitBreakerState.Isolated).Should().Be(3);

        ((int)RateLimiterType.SlidingWindow).Should().Be(0);
        ((int)RateLimiterType.FixedWindow).Should().Be(1);
        ((int)RateLimiterType.TokenBucket).Should().Be(2);
        ((int)RateLimiterType.Concurrency).Should().Be(3);
    }
}
