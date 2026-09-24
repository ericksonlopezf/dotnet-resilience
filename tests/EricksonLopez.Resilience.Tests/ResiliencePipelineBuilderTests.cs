// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Pipelines;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class ResiliencePipelineBuilderTests
{
    [Fact]
    public void Constructor_WithInvalidName_ThrowsArgumentException()
    {
        var act1 = () => new ResiliencePipelineBuilder(null!);
        var act2 = () => new ResiliencePipelineBuilder(string.Empty);
        var act3 = () => new ResiliencePipelineBuilder("   ");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();

        var act4 = () => new ResiliencePipelineBuilder<string>(null!);
        var act5 = () => new ResiliencePipelineBuilder<string>(string.Empty);
        var act6 = () => new ResiliencePipelineBuilder<string>("   ");

        act4.Should().Throw<ArgumentNullException>();
        act5.Should().Throw<ArgumentException>();
        act6.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetPipelineFactory_WithNull_ThrowsArgumentNullException()
    {
        var act1 = () => ResiliencePipelineBuilder.SetPipelineFactory(null!);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => ResiliencePipelineBuilder.SetTypedPipelineFactory<string>(null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Build_WithoutFactory_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("unconfigured-pipeline");
        var typedBuilder = new ResiliencePipelineBuilder<double>("typed-unconfigured");

        // Act
        var act1 = () => builder.Build();
        var act2 = () => typedBuilder.Build();

        // Assert
        act1.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot compile resilience pipeline*");
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot compile typed resilience pipeline*");
    }

    [Fact]
    public void Build_WithCustomFactory_InvokesRegisteredFactory()
    {
        // Arrange
        var mockPipeline = Substitute.For<IResiliencePipeline>();
        ResiliencePipelineBuilder.SetPipelineFactory(b => mockPipeline);
        var builder = new ResiliencePipelineBuilder("custom-compiled");

        // Act
        var compiled = builder.Build();

        // Assert
        compiled.Should().BeSameAs(mockPipeline);

        // Typed factory
        var mockTypedPipeline = Substitute.For<IResiliencePipeline<long>>();
        ResiliencePipelineBuilder.SetTypedPipelineFactory<long>(b => mockTypedPipeline);
        var typedBuilder = new ResiliencePipelineBuilder<long>("custom-typed-compiled");

        // Act
        var compiledTyped = typedBuilder.Build();

        // Assert
        compiledTyped.Should().BeSameAs(mockTypedPipeline);
    }

    [Fact]
    public void AddRetry_WithValidBoundaries_DoesNotThrow()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("retry-boundaries");

        // Act & Assert (MaxRetryAttempts = 0 is valid, Delay = Zero is valid, MaxDelay == Delay is valid)
        builder.AddRetry(options =>
        {
            options.MaxRetryAttempts = 0;
            options.Delay = TimeSpan.Zero;
            options.MaxDelay = TimeSpan.Zero;
            options.BackoffType = BackoffType.ExponentialWithJitter;
        });

        builder.Strategies.Should().ContainSingle();
        var strategy = builder.Strategies[0].Should().BeOfType<RetryStrategyOptions>().Which;
        strategy.MaxRetryAttempts.Should().Be(0);
        strategy.Delay.Should().Be(TimeSpan.Zero);
        strategy.MaxDelay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AddRetry_WithInvalidOptions_ThrowsResilienceConfigurationException_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder("test-policy");

        var actNull = () => builder.AddRetry((RetryStrategyOptions)null!);
        var actNullConfig = () => builder.AddRetry((Action<RetryStrategyOptions>)null!);
        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();

        var actNegAttempts = () => builder.AddRetry(opt => opt.MaxRetryAttempts = -1);
        actNegAttempts.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Retry MaxRetryAttempts must be greater than or equal to 0.");

        var actNegDelay = () => builder.AddRetry(opt => opt.Delay = TimeSpan.FromMilliseconds(-1));
        actNegDelay.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Retry Delay must be greater than or equal to TimeSpan.Zero.");

        var actMaxDelayLessThanDelay = () => builder.AddRetry(opt =>
        {
            opt.Delay = TimeSpan.FromSeconds(10);
            opt.MaxDelay = TimeSpan.FromSeconds(5);
        });
        actMaxDelayLessThanDelay.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Retry MaxDelay cannot be less than base Delay.");
    }

    [Fact]
    public void AddCircuitBreaker_WithValidBoundaries_DoesNotThrow()
    {
        var builder = new ResiliencePipelineBuilder("cb-boundaries");

        // FailureRatio == 1.0 is valid, MinimumThroughput == 1 is valid
        builder.AddCircuitBreaker(options =>
        {
            options.FailureRatio = 1.0;
            options.MinimumThroughput = 1;
            options.SamplingDuration = TimeSpan.FromMilliseconds(1);
            options.BreakDuration = TimeSpan.FromMilliseconds(500);
        });

        builder.Strategies.Should().ContainSingle();
        var strategy = builder.Strategies[0].Should().BeOfType<CircuitBreakerStrategyOptions>().Which;
        strategy.FailureRatio.Should().Be(1.0);
        strategy.MinimumThroughput.Should().Be(1);
    }

    [Fact]
    public void AddCircuitBreaker_WithInvalidOptions_ThrowsResilienceConfigurationException_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder("test-policy");

        var actNull = () => builder.AddCircuitBreaker((CircuitBreakerStrategyOptions)null!);
        var actNullConfig = () => builder.AddCircuitBreaker((Action<CircuitBreakerStrategyOptions>)null!);
        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();

        var actRatioZero = () => builder.AddCircuitBreaker(opt => opt.FailureRatio = 0.0);
        actRatioZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker FailureRatio must be greater than 0.0 and less than or equal to 1.0.");

        var actRatioExcess = () => builder.AddCircuitBreaker(opt => opt.FailureRatio = 1.01);
        actRatioExcess.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker FailureRatio must be greater than 0.0 and less than or equal to 1.0.");

        var actMinThroughputZero = () => builder.AddCircuitBreaker(opt => opt.MinimumThroughput = 0);
        actMinThroughputZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker MinimumThroughput must be greater than 0.");

        var actSamplingZero = () => builder.AddCircuitBreaker(opt => opt.SamplingDuration = TimeSpan.Zero);
        actSamplingZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker SamplingDuration must be greater than TimeSpan.Zero.");

        var actBreakZero = () => builder.AddCircuitBreaker(opt => opt.BreakDuration = TimeSpan.FromMilliseconds(499));
        actBreakZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker BreakDuration must be greater than or equal to 500 milliseconds.");
    }

    [Fact]
    public void AddTimeout_WithValidOptions_AddsStrategySuccessfully()
    {
        var builder = new ResiliencePipelineBuilder("timeout-policy");

        builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromSeconds(7));

        builder.Strategies.Should().ContainSingle();
        var strategy = builder.Strategies[0].Should().BeOfType<TimeoutStrategyOptions>().Which;
        strategy.Timeout.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void AddTimeout_WithInvalidOptions_ThrowsResilienceConfigurationException_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder("test-policy");

        var actNull = () => builder.AddTimeout((TimeoutStrategyOptions)null!);
        var actNullConfig = () => builder.AddTimeout((Action<TimeoutStrategyOptions>)null!);
        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();

        var actZeroTimeout = () => builder.AddTimeout(opt => opt.Timeout = TimeSpan.Zero);
        actZeroTimeout.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Timeout must be greater than TimeSpan.Zero.");

        var actNegTimeout = () => builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromSeconds(-1));
        actNegTimeout.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Timeout must be greater than TimeSpan.Zero.");
    }

    [Fact]
    public void AddRateLimiter_WithValidBoundaries_DoesNotThrow()
    {
        var builder = new ResiliencePipelineBuilder("rate-limiter-boundaries");

        // PermitLimit == 1 is valid, QueueLimit == 0 is valid, Window > Zero is valid
        builder.AddRateLimiter(opt =>
        {
            opt.PermitLimit = 1;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMilliseconds(1);
        });

        builder.Strategies.Should().ContainSingle();
        var strategy = builder.Strategies[0].Should().BeOfType<RateLimiterStrategyOptions>().Which;
        strategy.PermitLimit.Should().Be(1);
        strategy.QueueLimit.Should().Be(0);
    }

    [Fact]
    public void AddRateLimiter_WithInvalidOptions_ThrowsResilienceConfigurationException_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder("test-policy");

        var actNull = () => builder.AddRateLimiter((RateLimiterStrategyOptions)null!);
        var actNullConfig = () => builder.AddRateLimiter((Action<RateLimiterStrategyOptions>)null!);
        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();

        var actPermitZero = () => builder.AddRateLimiter(opt => opt.PermitLimit = 0);
        actPermitZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("RateLimiter PermitLimit must be greater than 0.");

        var actNegQueue = () => builder.AddRateLimiter(opt => opt.QueueLimit = -1);
        actNegQueue.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("RateLimiter QueueLimit cannot be negative.");

        var actZeroWindow = () => builder.AddRateLimiter(opt => opt.Window = TimeSpan.Zero);
        actZeroWindow.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("RateLimiter Window must be greater than TimeSpan.Zero.");
    }

    [Fact]
    public void AddHedging_WithValidBoundaries_DoesNotThrow()
    {
        var builder = new ResiliencePipelineBuilder("hedging-boundaries");

        // MaxHedgedAttempts == 1 is valid, Delay == Zero is valid
        builder.AddHedging(opt =>
        {
            opt.MaxHedgedAttempts = 1;
            opt.Delay = TimeSpan.Zero;
        });

        builder.Strategies.Should().ContainSingle();
        var strategy = builder.Strategies[0].Should().BeOfType<HedgingStrategyOptions>().Which;
        strategy.MaxHedgedAttempts.Should().Be(1);
        strategy.Delay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void AddHedging_WithInvalidOptions_ThrowsResilienceConfigurationException_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder("test-policy");

        var actNull = () => builder.AddHedging((HedgingStrategyOptions)null!);
        var actNullConfig = () => builder.AddHedging((Action<HedgingStrategyOptions>)null!);
        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();

        var actZeroAttempts = () => builder.AddHedging(opt => opt.MaxHedgedAttempts = 0);
        actZeroAttempts.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Hedging MaxHedgedAttempts must be greater than 0.");

        var actNegDelay = () => builder.AddHedging(opt => opt.Delay = TimeSpan.FromSeconds(-1));
        actNegDelay.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Hedging Delay cannot be negative.");
    }

    [Fact]
    public void TypedPipelineBuilder_ConfiguresAllStrategiesAndValidates()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder<string>("typed-policy");

        // Act
        builder
            .AddRetry(opt => opt.MaxRetryAttempts = 2)
            .AddCircuitBreaker(opt => opt.MinimumThroughput = 5)
            .AddTimeout(opt => opt.Timeout = TimeSpan.FromSeconds(3))
            .AddRateLimiter(opt => opt.PermitLimit = 50)
            .AddFallback(opt => opt.FallbackAction = _ => ValueTask.FromResult("fallback"))
            .AddHedging(opt => opt.MaxHedgedAttempts = 2);

        // Assert
        builder.Name.Should().Be("typed-policy");
        builder.Strategies.Should().HaveCount(6);
    }

    [Fact]
    public void TypedPipelineBuilder_AddFallback_WithInvalidOptions_ThrowsExceptions_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-policy");

        var actNull = () => builder.AddFallback((FallbackStrategyOptions<string>)null!);
        var actNullConfig = () => builder.AddFallback((Action<FallbackStrategyOptions<string>>)null!);
        var actNullAction = () => builder.AddFallback(opt => opt.FallbackAction = null);

        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();
        actNullAction.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("FallbackStrategyOptions must specify a valid FallbackAction delegate.");
    }

    [Fact]
    public void TypedPipelineBuilder_AddHedging_WithValidBoundaries_AndInvalidOptions_ValidatesCorrectly()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-hedging");

        // Valid boundary: MaxHedgedAttempts == 1, Delay == Zero
        builder.AddHedging(opt =>
        {
            opt.MaxHedgedAttempts = 1;
            opt.Delay = TimeSpan.Zero;
        });
        builder.Strategies.Should().ContainSingle();

        var actNull = () => builder.AddHedging((HedgingStrategyOptions<string>)null!);
        var actNullConfig = () => builder.AddHedging((Action<HedgingStrategyOptions<string>>)null!);
        var actZeroAttempts = () => builder.AddHedging(opt => opt.MaxHedgedAttempts = 0);
        var actNegDelay = () => builder.AddHedging(opt => opt.Delay = TimeSpan.FromSeconds(-1));

        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();
        actZeroAttempts.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Hedging MaxHedgedAttempts must be greater than 0.");
        actNegDelay.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Hedging Delay cannot be negative.");
    }

    [Fact]
    public void TypedPipelineBuilder_AddRateLimiter_WithValidBoundaries_AndInvalidOptions_ValidatesCorrectly()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-rate-limiter");

        // Valid boundary: PermitLimit == 1, QueueLimit == 0, Window > Zero
        builder.AddRateLimiter(opt =>
        {
            opt.PermitLimit = 1;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromSeconds(1);
        });
        builder.Strategies.Should().ContainSingle();

        var customLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 1,
            QueueLimit = 0
        });

        builder.AddRateLimiter(opt =>
        {
            opt.CustomRateLimiter = customLimiter;
            opt.PermitLimit = -100; // ignored when custom limiter is set
        });
        builder.Strategies.Should().HaveCount(2);

        var actNull = () => builder.AddRateLimiter((RateLimiterStrategyOptions)null!);
        var actNullConfig = () => builder.AddRateLimiter((Action<RateLimiterStrategyOptions>)null!);
        var actZeroPermit = () => builder.AddRateLimiter(opt => opt.PermitLimit = 0);
        var actNegQueue = () => builder.AddRateLimiter(opt => opt.QueueLimit = -1);
        var actZeroWindow = () => builder.AddRateLimiter(opt => opt.Window = TimeSpan.Zero);

        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();
        actZeroPermit.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("RateLimiter PermitLimit must be greater than 0.");
        actNegQueue.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("RateLimiter QueueLimit cannot be negative.");
        actZeroWindow.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("RateLimiter Window must be greater than TimeSpan.Zero.");
    }

    [Fact]
    public void TypedPipelineBuilder_AddTimeout_WithInvalidOptions_ThrowsResilienceConfigurationException_WithExactMessages()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-timeout");

        var actNull = () => builder.AddTimeout((TimeoutStrategyOptions)null!);
        var actNullConfig = () => builder.AddTimeout((Action<TimeoutStrategyOptions>)null!);
        var actZero = () => builder.AddTimeout(opt => opt.Timeout = TimeSpan.Zero);
        var actNeg = () => builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromSeconds(-1));

        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();
        actZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Timeout must be greater than TimeSpan.Zero.");
        actNeg.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Timeout must be greater than TimeSpan.Zero.");
    }

    [Fact]
    public void TypedPipelineBuilder_AddCircuitBreaker_WithValidBoundaries_AndInvalidOptions_ValidatesCorrectly()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-cb");

        // Valid boundary: FailureRatio == 1.0, MinimumThroughput == 1, SamplingDuration > Zero, BreakDuration >= 500ms
        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 1.0;
            opt.MinimumThroughput = 1;
            opt.SamplingDuration = TimeSpan.FromMilliseconds(5);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
        });
        builder.Strategies.Should().ContainSingle();

        var actNull = () => builder.AddCircuitBreaker((CircuitBreakerStrategyOptions)null!);
        var actNullConfig = () => builder.AddCircuitBreaker((Action<CircuitBreakerStrategyOptions>)null!);
        var actRatioZero = () => builder.AddCircuitBreaker(opt => opt.FailureRatio = 0.0);
        var actRatioHigh = () => builder.AddCircuitBreaker(opt => opt.FailureRatio = 1.01);
        var actThroughput = () => builder.AddCircuitBreaker(opt => opt.MinimumThroughput = 0);
        var actSampling = () => builder.AddCircuitBreaker(opt => opt.SamplingDuration = TimeSpan.Zero);
        var actBreak = () => builder.AddCircuitBreaker(opt => opt.BreakDuration = TimeSpan.FromMilliseconds(499));

        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();
        actRatioZero.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker FailureRatio must be greater than 0.0 and less than or equal to 1.0.");
        actRatioHigh.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker FailureRatio must be greater than 0.0 and less than or equal to 1.0.");
        actThroughput.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker MinimumThroughput must be greater than 0.");
        actSampling.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker SamplingDuration must be greater than TimeSpan.Zero.");
        actBreak.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("CircuitBreaker BreakDuration must be greater than or equal to 500 milliseconds.");
    }

    [Fact]
    public void TypedPipelineBuilder_AddRetry_WithValidBoundaries_AndInvalidOptions_ValidatesCorrectly()
    {
        var builder = new ResiliencePipelineBuilder<string>("typed-retry");

        // Valid boundary: MaxRetryAttempts == 0, Delay == Zero, MaxDelay == Delay
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 0;
            opt.Delay = TimeSpan.Zero;
            opt.MaxDelay = TimeSpan.Zero;
        });
        builder.Strategies.Should().ContainSingle();

        var actNull = () => builder.AddRetry((RetryStrategyOptions)null!);
        var actNullConfig = () => builder.AddRetry((Action<RetryStrategyOptions>)null!);
        var actNegAttempts = () => builder.AddRetry(opt => opt.MaxRetryAttempts = -1);
        var actNegDelay = () => builder.AddRetry(opt => opt.Delay = TimeSpan.FromSeconds(-1));
        var actMaxDelay = () => builder.AddRetry(opt =>
        {
            opt.Delay = TimeSpan.FromSeconds(10);
            opt.MaxDelay = TimeSpan.FromSeconds(2);
        });

        actNull.Should().Throw<ArgumentNullException>();
        actNullConfig.Should().Throw<ArgumentNullException>();
        actNegAttempts.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Retry MaxRetryAttempts must be greater than or equal to 0.");
        actNegDelay.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Retry Delay must be greater than or equal to TimeSpan.Zero.");
        actMaxDelay.Should().Throw<ResilienceConfigurationException>()
            .WithMessage("Retry MaxDelay cannot be less than base Delay.");
    }
}
