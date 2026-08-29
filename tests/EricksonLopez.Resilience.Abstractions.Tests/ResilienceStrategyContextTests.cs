// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Options;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class ResilienceStrategyContextTests
{
    [Fact]
    public void CircuitBreakerStateContext_WithAllParameters_InitializesCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("cb-policy");
        var ex = new InvalidOperationException("boom");
        var duration = TimeSpan.FromSeconds(15);

        // Act
        var stateContext = new CircuitBreakerStateContext(
            context,
            CircuitBreakerState.Open,
            duration,
            ex);

        // Assert
        stateContext.ResilienceContext.Should().BeSameAs(context);
        stateContext.State.Should().Be(CircuitBreakerState.Open);
        stateContext.BreakDuration.Should().Be(duration);
        stateContext.LastException.Should().BeSameAs(ex);
    }

    [Fact]
    public void CircuitBreakerStateContext_WithNullOptionalParameters_InitializesCorrectly()
    {
        // Act
        var stateContext = new CircuitBreakerStateContext(
            null,
            CircuitBreakerState.Closed);

        // Assert
        stateContext.ResilienceContext.Should().BeNull();
        stateContext.State.Should().Be(CircuitBreakerState.Closed);
        stateContext.BreakDuration.Should().BeNull();
        stateContext.LastException.Should().BeNull();
    }

    [Fact]
    public void FallbackContext_WithValidContext_InitializesCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("fallback-policy");
        var ex = new TimeoutException();

        // Act
        var fallbackContext = new FallbackContext(context, ex, "sample-result");

        // Assert
        fallbackContext.Context.Should().BeSameAs(context);
        fallbackContext.Exception.Should().BeSameAs(ex);
        fallbackContext.Result.Should().Be("sample-result");
    }

    [Fact]
    public void FallbackContext_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FallbackContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HedgingContext_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("hedging-policy");

        // Act
        var hedgingContext = new HedgingContext(context, 2);

        // Assert
        hedgingContext.ResilienceContext.Should().BeSameAs(context);
        hedgingContext.AttemptNumber.Should().Be(2);
    }

    [Fact]
    public void HedgingContext_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new HedgingContext(null!, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RateLimiterContext_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("rl-policy");
        var retryAfter = TimeSpan.FromSeconds(3);

        // Act
        var rlContext = new RateLimiterContext(context, retryAfter);

        // Assert
        rlContext.ResilienceContext.Should().BeSameAs(context);
        rlContext.RetryAfter.Should().Be(retryAfter);
    }

    [Fact]
    public void RateLimiterContext_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new RateLimiterContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RetryAttemptContext_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("retry-policy");
        var delay = TimeSpan.FromSeconds(1);
        var ex = new InvalidOperationException();

        // Act
        var retryContext = new RetryAttemptContext(context, 3, delay, ex, "bad-result");

        // Assert
        retryContext.ResilienceContext.Should().BeSameAs(context);
        retryContext.AttemptNumber.Should().Be(3);
        retryContext.Delay.Should().Be(delay);
        retryContext.Exception.Should().BeSameAs(ex);
        retryContext.Result.Should().Be("bad-result");
    }

    [Fact]
    public void RetryAttemptContext_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new RetryAttemptContext(null!, 1, TimeSpan.Zero);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TimeoutContext_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("timeout-policy");
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        var timeoutContext = new TimeoutContext(context, timeout);

        // Assert
        timeoutContext.ResilienceContext.Should().BeSameAs(context);
        timeoutContext.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void TimeoutContext_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TimeoutContext(null!, TimeSpan.FromSeconds(1));
        act.Should().Throw<ArgumentNullException>();
    }
}
