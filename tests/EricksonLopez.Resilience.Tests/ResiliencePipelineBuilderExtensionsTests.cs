// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Net.Sockets;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Result;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class ResiliencePipelineBuilderExtensionsTests
{
    [Fact]
    public void AddResultRetry_WithNullBuilder_ThrowsArgumentNullException()
    {
        IResiliencePipelineBuilder builder = null!;
        var act = () => builder.AddResultRetry();
        act.Should().Throw<ArgumentNullException>();

        ResiliencePipelineBuilder<string> typedBuilder = null!;
        var actTyped = () => typedBuilder.AddResultRetry();
        actTyped.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddResultRetry_WithDefaultClassifier_ConfiguresPredicatesCorrectly()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("result-retry");

        // Act
        builder.AddResultRetry(opt => opt.MaxRetryAttempts = 4);

        // Assert
        builder.Strategies.Should().ContainSingle();
        var retry = builder.Strategies[0].Should().BeOfType<RetryStrategyOptions>().Which;
        retry.MaxRetryAttempts.Should().Be(4);
        retry.ShouldHandleException.Should().NotBeNull();
        retry.ShouldHandleResult.Should().NotBeNull();

        retry.ShouldHandleException!(new SocketException()).Should().BeTrue();
        retry.ShouldHandleException!(new ArgumentException()).Should().BeFalse();

        var successResult = Result<string>.Success("ok");
        var failResult = Result<string>.Failure(new Error("CODE", "msg", ErrorType.Unavailable));
        retry.ShouldHandleResult!(successResult).Should().BeFalse();
        retry.ShouldHandleResult!(failResult).Should().BeTrue();
    }

    [Fact]
    public void AddResultRetry_WithCustomClassifier_UsesProvidedClassifier()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("custom-classifier");
        var mockClassifier = Substitute.For<IResultRetryClassifier>();
        var ex = new InvalidOperationException();
        mockClassifier.ClassifyException(ex).Returns(RetryabilityDecision.Retry);

        // Act
        builder.AddResultRetry(classifier: mockClassifier);

        // Assert
        var retry = builder.Strategies[0].Should().BeOfType<RetryStrategyOptions>().Which;
        retry.ShouldHandleException!(ex).Should().BeTrue();
        mockClassifier.Received(1).ClassifyException(ex);
    }

    [Fact]
    public void AddStandardResilience_WithNullBuilder_ThrowsArgumentNullException()
    {
        IResiliencePipelineBuilder builder = null!;
        var act = () => builder.AddStandardResilience();
        act.Should().Throw<ArgumentNullException>();

        ResiliencePipelineBuilder<int> typedBuilder = null!;
        var actTyped = () => typedBuilder.AddStandardResilience();
        actTyped.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStandardResilience_AddsTimeoutRetryAndCircuitBreaker_WithConfiguredProperties()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("standard-resilience");

        // Act
        var returned = builder.AddStandardResilience();

        // Assert
        returned.Should().BeSameAs(builder);
        builder.Strategies.Should().HaveCount(3);
        var timeout = builder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        var retry = builder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        var cb = builder.Strategies[2].Should().BeOfType<CircuitBreakerStrategyOptions>().Which;

        timeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        retry.MaxRetryAttempts.Should().Be(3);
        retry.Delay.Should().Be(TimeSpan.FromSeconds(1));
        retry.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        cb.FailureRatio.Should().Be(0.5);
        cb.MinimumThroughput.Should().Be(20);
        cb.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        cb.BreakDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void AddDatabaseResilience_WithDefaultAndCustomParameters_ConfiguresCorrectly()
    {
        // Arrange
        var builderDefault = new ResiliencePipelineBuilder("db-default");
        var builderCustom = new ResiliencePipelineBuilder("db-custom");

        // Act
        builderDefault.AddDatabaseResilience();
        builderCustom.AddDatabaseResilience(timeout: TimeSpan.FromSeconds(5), maxRetries: 7);

        // Assert
        var timeoutDef = builderDefault.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        var retryDef = builderDefault.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        timeoutDef.Timeout.Should().Be(TimeSpan.FromSeconds(15));
        retryDef.MaxRetryAttempts.Should().Be(3);
        retryDef.Delay.Should().Be(TimeSpan.FromMilliseconds(200));
        retryDef.MaxDelay.Should().Be(TimeSpan.FromSeconds(2));
        retryDef.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);

        var timeoutCust = builderCustom.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        var retryCust = builderCustom.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        timeoutCust.Timeout.Should().Be(TimeSpan.FromSeconds(5));
        retryCust.MaxRetryAttempts.Should().Be(7);
        retryCust.Delay.Should().Be(TimeSpan.FromMilliseconds(200));
        retryCust.MaxDelay.Should().Be(TimeSpan.FromSeconds(2));
        retryCust.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
    }

    [Fact]
    public void AddDatabaseResilience_WithNullBuilder_ThrowsArgumentNullException()
    {
        IResiliencePipelineBuilder builder = null!;
        var act = () => builder.AddDatabaseResilience();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddTimeout_TimeSpanOverload_AddsTimeoutOptions()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("timeout-policy");

        // Act
        builder.AddTimeout(TimeSpan.FromSeconds(8));

        // Assert
        builder.Strategies.Should().ContainSingle();
        var timeout = builder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        timeout.Timeout.Should().Be(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void AddTimeout_WithNullBuilder_ThrowsArgumentNullException()
    {
        IResiliencePipelineBuilder builder = null!;
        var act = () => builder.AddTimeout(TimeSpan.FromSeconds(5));
        act.Should().Throw<ArgumentNullException>();

        ResiliencePipelineBuilder<string> typedBuilder = null!;
        var actTyped = () => typedBuilder.AddTimeout(TimeSpan.FromSeconds(5));
        actTyped.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TypedExtensions_AddTimeout_ResultRetry_AndStandardResilience_WorkCorrectly()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-pipeline");

        // Act
        builder.AddTimeout(TimeSpan.FromSeconds(12));
        builder.AddResultRetry(opt => opt.MaxRetryAttempts = 2);

        // Assert
        builder.Strategies.Should().HaveCount(2);
        var timeout = builder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        var retry = builder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;

        timeout.Timeout.Should().Be(TimeSpan.FromSeconds(12));
        retry.MaxRetryAttempts.Should().Be(2);
        retry.ShouldHandleException!(new IOException()).Should().BeTrue();
        retry.ShouldHandleResult!(Result<string>.Success("ok")).Should().BeFalse();

        var standardBuilder = new ResiliencePipelineBuilder<int>("typed-standard");
        var returned = standardBuilder.AddStandardResilience();
        returned.Should().BeSameAs(standardBuilder);
        standardBuilder.Strategies.Should().HaveCount(3);
        var stdTimeout = standardBuilder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        var stdRetry = standardBuilder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        var stdCb = standardBuilder.Strategies[2].Should().BeOfType<CircuitBreakerStrategyOptions>().Which;

        stdTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        stdRetry.MaxRetryAttempts.Should().Be(3);
        stdRetry.Delay.Should().Be(TimeSpan.FromSeconds(1));
        stdRetry.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        stdCb.FailureRatio.Should().Be(0.5);
        stdCb.MinimumThroughput.Should().Be(20);
        stdCb.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        stdCb.BreakDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void TypedExtensions_AddResultRetry_WithCustomClassifier_WorksCorrectly()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-custom-classifier");
        var mockClassifier = Substitute.For<IResultRetryClassifier>();
        var ex = new TimeoutException();
        mockClassifier.ClassifyException(ex).Returns(RetryabilityDecision.Retry);

        // Act
        builder.AddResultRetry(classifier: mockClassifier);

        // Assert
        var retry = builder.Strategies[0].Should().BeOfType<RetryStrategyOptions>().Which;
        retry.ShouldHandleException!(ex).Should().BeTrue();
        mockClassifier.Received(1).ClassifyException(ex);
    }

    [Fact]
    public void AddDatabaseResilience_Untyped_ConfiguresStrategiesCorrectly()
    {
        IResiliencePipelineBuilder nullBuilder = null!;
        var actNull = () => nullBuilder.AddDatabaseResilience();
        actNull.Should().Throw<ArgumentNullException>();

        // Default parameters
        var defaultBuilder = new ResiliencePipelineBuilder("db-default");
        var returnedDefault = defaultBuilder.AddDatabaseResilience();
        returnedDefault.Should().BeSameAs(defaultBuilder);
        defaultBuilder.Strategies.Should().HaveCount(2);

        var timeoutOpt = defaultBuilder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        timeoutOpt.Timeout.Should().Be(TimeSpan.FromSeconds(15));

        var retryOpt = defaultBuilder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        retryOpt.MaxRetryAttempts.Should().Be(3);
        retryOpt.Delay.Should().Be(TimeSpan.FromMilliseconds(200));
        retryOpt.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        retryOpt.MaxDelay.Should().Be(TimeSpan.FromSeconds(2));

        // Custom parameters
        var customBuilder = new ResiliencePipelineBuilder("db-custom");
        customBuilder.AddDatabaseResilience(TimeSpan.FromSeconds(25), maxRetries: 5);
        customBuilder.Strategies.Should().HaveCount(2);

        var customTimeout = customBuilder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        customTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(25));

        var customRetry = customBuilder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        customRetry.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void AddDatabaseResilience_Typed_ConfiguresStrategiesCorrectly()
    {
        ResiliencePipelineBuilder<string> nullBuilder = null!;
        var actNull = () => nullBuilder.AddDatabaseResilience();
        actNull.Should().Throw<ArgumentNullException>();

        // Default parameters
        var defaultBuilder = new ResiliencePipelineBuilder<string>("db-typed-default");
        var returnedDefault = defaultBuilder.AddDatabaseResilience();
        returnedDefault.Should().BeSameAs(defaultBuilder);
        defaultBuilder.Strategies.Should().HaveCount(2);

        var timeoutOpt = defaultBuilder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        timeoutOpt.Timeout.Should().Be(TimeSpan.FromSeconds(15));

        var retryOpt = defaultBuilder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        retryOpt.MaxRetryAttempts.Should().Be(3);
        retryOpt.Delay.Should().Be(TimeSpan.FromMilliseconds(200));
        retryOpt.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        retryOpt.MaxDelay.Should().Be(TimeSpan.FromSeconds(2));

        // Custom parameters
        var customBuilder = new ResiliencePipelineBuilder<string>("db-typed-custom");
        customBuilder.AddDatabaseResilience(TimeSpan.FromSeconds(40), maxRetries: 6);
        customBuilder.Strategies.Should().HaveCount(2);

        var customTimeout = customBuilder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        customTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(40));

        var customRetry = customBuilder.Strategies[1].Should().BeOfType<RetryStrategyOptions>().Which;
        customRetry.MaxRetryAttempts.Should().Be(6);
    }
}
