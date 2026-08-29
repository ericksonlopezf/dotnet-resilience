// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Exceptions;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class ResilienceExceptionsTests
{
    [Fact]
    public void ResilienceException_ParameterlessConstructor_SetsDefaultMessage()
    {
        // Act
        var ex = new ResilienceException();

        // Assert
        ex.Message.Should().Be("A resilience fault occurred during operation execution.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void ResilienceException_WithMessage_SetsMessage()
    {
        // Act
        var ex = new ResilienceException("custom error");

        // Assert
        ex.Message.Should().Be("custom error");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void ResilienceException_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var inner = new InvalidOperationException("inner error");

        // Act
        var ex = new ResilienceException("custom error", inner);

        // Assert
        ex.Message.Should().Be("custom error");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void CircuitBrokenException_WithoutRetryAfter_FormatsMessageCorrectly()
    {
        // Act
        var ex = new CircuitBrokenException("payment-policy");

        // Assert
        ex.PolicyName.Should().Be("payment-policy");
        ex.RetryAfter.Should().BeNull();
        ex.Message.Should().Be("Execution rejected because the circuit breaker for policy 'payment-policy' is currently Open.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void CircuitBrokenException_WithRetryAfter_FormatsMessageCorrectly()
    {
        // Act
        var ex = new CircuitBrokenException("inventory-policy", TimeSpan.FromSeconds(15.5));

        // Assert
        ex.PolicyName.Should().Be("inventory-policy");
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(15.5));
        ex.Message.Should().Be("Execution rejected because the circuit breaker for policy 'inventory-policy' is currently Open. Try again after 15.5s.");
    }

    [Fact]
    public void CircuitBrokenException_WithInnerException_PreservesAllProperties()
    {
        // Arrange
        var inner = new TimeoutException("timeout");

        // Act
        var ex = new CircuitBrokenException("auth-policy", TimeSpan.FromSeconds(3.0), inner);

        // Assert
        ex.PolicyName.Should().Be("auth-policy");
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(3.0));
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("Execution rejected because the circuit breaker for policy 'auth-policy' is currently Open. Try again after 3.0s.");
    }

    [Fact]
    public void CircuitBrokenException_WithInnerExceptionAndNullRetryAfter_PreservesAllProperties()
    {
        // Arrange
        var inner = new TimeoutException("timeout");

        // Act
        var ex = new CircuitBrokenException("auth-policy", null, inner);

        // Assert
        ex.PolicyName.Should().Be("auth-policy");
        ex.RetryAfter.Should().BeNull();
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("Execution rejected because the circuit breaker for policy 'auth-policy' is currently Open.");
    }

    [Fact]
    public void RateLimitRejectedException_WithoutRetryAfter_FormatsMessageCorrectly()
    {
        // Act
        var ex = new RateLimitRejectedException("api-policy");

        // Assert
        ex.PolicyName.Should().Be("api-policy");
        ex.RetryAfter.Should().BeNull();
        ex.Message.Should().Be("Execution rejected by rate limiter in policy 'api-policy'.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void RateLimitRejectedException_WithRetryAfter_FormatsMessageCorrectly()
    {
        // Act
        var ex = new RateLimitRejectedException("api-policy", TimeSpan.FromSeconds(2.5));

        // Assert
        ex.PolicyName.Should().Be("api-policy");
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(2.5));
        ex.Message.Should().Be("Execution rejected by rate limiter in policy 'api-policy'. Retry after 2.5s.");
    }

    [Fact]
    public void RateLimitRejectedException_WithInnerException_PreservesAllProperties()
    {
        // Arrange
        var inner = new InvalidOperationException("rate exceeded");

        // Act
        var ex = new RateLimitRejectedException("api-policy", TimeSpan.FromSeconds(1.0), inner);

        // Assert
        ex.PolicyName.Should().Be("api-policy");
        ex.RetryAfter.Should().Be(TimeSpan.FromSeconds(1.0));
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("Execution rejected by rate limiter in policy 'api-policy'. Retry after 1.0s.");
    }

    [Fact]
    public void RateLimitRejectedException_WithInnerExceptionAndNullRetryAfter_PreservesAllProperties()
    {
        // Arrange
        var inner = new InvalidOperationException("rate exceeded");

        // Act
        var ex = new RateLimitRejectedException("api-policy", null, inner);

        // Assert
        ex.PolicyName.Should().Be("api-policy");
        ex.RetryAfter.Should().BeNull();
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("Execution rejected by rate limiter in policy 'api-policy'.");
    }

    [Fact]
    public void ResiliencePolicyNotFoundException_StoresPolicyName()
    {
        // Act
        var ex = new ResiliencePolicyNotFoundException("unknown-policy");

        // Assert
        ex.PolicyName.Should().Be("unknown-policy");
        ex.Message.Should().Be("Resilience policy 'unknown-policy' was not found in the registry. Ensure it is configured and registered during application startup.");
    }

    [Fact]
    public void ResilienceConfigurationException_WithMessageOnly_StoresMessage()
    {
        // Act
        var ex = new ResilienceConfigurationException("Invalid configuration value");

        // Assert
        ex.Message.Should().Be("Invalid configuration value");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void ResilienceConfigurationException_StoresMessageAndInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Invalid state");

        // Act
        var ex = new ResilienceConfigurationException("Invalid configuration", inner);

        // Assert
        ex.Message.Should().Be("Invalid configuration");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ResilienceTimeoutException_WithoutPolicyName_FormatsMessageCorrectly()
    {
        // Act
        var ex = new ResilienceTimeoutException(TimeSpan.FromMilliseconds(500));

        // Assert
        ex.Timeout.Should().Be(TimeSpan.FromMilliseconds(500));
        ex.PolicyName.Should().BeNull();
        ex.Message.Should().Be("Operation execution timed out after 500ms in policy 'Unknown'.");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void ResilienceTimeoutException_WithPolicyName_FormatsMessageCorrectly()
    {
        // Act
        var ex = new ResilienceTimeoutException(TimeSpan.FromSeconds(5), "payment-policy");

        // Assert
        ex.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        ex.PolicyName.Should().Be("payment-policy");
        ex.Message.Should().Be("Operation execution timed out after 5000ms in policy 'payment-policy'.");
    }

    [Fact]
    public void ResilienceTimeoutException_WithInnerException_PreservesAllProperties()
    {
        // Arrange
        var inner = new OperationCanceledException();

        // Act
        var ex = new ResilienceTimeoutException(TimeSpan.FromSeconds(2), "billing-policy", inner);

        // Assert
        ex.Timeout.Should().Be(TimeSpan.FromSeconds(2));
        ex.PolicyName.Should().Be("billing-policy");
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("Operation execution timed out after 2000ms in policy 'billing-policy'.");
    }

    [Fact]
    public void ResilienceTimeoutException_WithInnerExceptionAndNullPolicyName_PreservesAllProperties()
    {
        // Arrange
        var inner = new OperationCanceledException();

        // Act
        var ex = new ResilienceTimeoutException(TimeSpan.FromSeconds(1), null, inner);

        // Assert
        ex.Timeout.Should().Be(TimeSpan.FromSeconds(1));
        ex.PolicyName.Should().BeNull();
        ex.InnerException.Should().BeSameAs(inner);
        ex.Message.Should().Be("Operation execution timed out after 1000ms in policy 'Unknown'.");
    }
}
