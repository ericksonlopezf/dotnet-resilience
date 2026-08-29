// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.DependencyInjection.Tests;

public sealed class ResilienceConfigurationExtensionsTests
{
    [Fact]
    public void AddResiliencePolicyFromConfiguration_WithNullSection_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Action act = () => services.AddResiliencePolicyFromConfiguration("test-policy", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddResiliencePolicyFromConfiguration_WithTimeoutSection_EnforcesTimeout()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Resilience:MyTimeoutPolicy:Timeout:TimeoutMilliseconds", "30" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = configuration.GetSection("Resilience:MyTimeoutPolicy");

        var services = new ServiceCollection();
        services.AddResiliencePolicyFromConfiguration("timeout-policy", section);

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<IResiliencePipelineRegistry>();
        var pipeline = registry.GetPipeline("timeout-policy");

        // Act & Assert
        Func<Task> act = async () =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(200, ct);
                return "TooSlow";
            });
        };

        await act.Should().ThrowAsync<ResilienceTimeoutException>();
    }

    [Fact]
    public async Task AddResiliencePolicyFromConfiguration_WithRetrySection_ExecutesRetries()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Resilience:MyRetryPolicy:Retry:MaxRetryAttempts", "3" },
            { "Resilience:MyRetryPolicy:Retry:DelayMilliseconds", "10" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = configuration.GetSection("Resilience:MyRetryPolicy");

        var services = new ServiceCollection();
        services.AddResiliencePolicyFromConfiguration("retry-policy", section);

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<IResiliencePipelineRegistry>();
        var pipeline = registry.GetPipeline("retry-policy");

        // Act
        var attempts = 0;
        var result = await pipeline.ExecuteAsync(ct =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new TimeoutException("transient fail");
            }
            return ValueTask.FromResult("RetrySuccess");
        });

        // Assert
        result.Should().Be("RetrySuccess");
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task AddResiliencePolicyFromConfiguration_WithCircuitBreakerSection_BreaksCircuitOnFailures()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Resilience:MyCbPolicy:CircuitBreaker:FailureRatio", "0.5" },
            { "Resilience:MyCbPolicy:CircuitBreaker:MinimumThroughput", "2" },
            { "Resilience:MyCbPolicy:CircuitBreaker:SamplingDurationSeconds", "10" },
            { "Resilience:MyCbPolicy:CircuitBreaker:BreakDurationSeconds", "10" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = configuration.GetSection("Resilience:MyCbPolicy");

        var services = new ServiceCollection();
        services.AddResiliencePolicyFromConfiguration("cb-policy", section);

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<IResiliencePipelineRegistry>();
        var pipeline = registry.GetPipeline("cb-policy");

        // Act & Assert
        Func<Task> failAct = async () =>
        {
            await pipeline.ExecuteAsync(ct => throw new TimeoutException("Fail"));
        };

        await failAct.Should().ThrowAsync<TimeoutException>();
        await failAct.Should().ThrowAsync<TimeoutException>();

        // 3rd attempt: circuit is now broken
        Func<Task> brokenAct = async () =>
        {
            await pipeline.ExecuteAsync(ct => ValueTask.FromResult("ok"));
        };
        await brokenAct.Should().ThrowAsync<CircuitBrokenException>();
    }

    [Fact]
    public async Task AddResiliencePolicyFromConfiguration_WithRateLimiterSection_RejectsExcessRequests()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Resilience:MyRatePolicy:RateLimiter:PermitLimit", "1" },
            { "Resilience:MyRatePolicy:RateLimiter:QueueLimit", "0" },
            { "Resilience:MyRatePolicy:RateLimiter:WindowSeconds", "60" },
            { "Resilience:MyRatePolicy:RateLimiter:LimiterType", "fixedwindow" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = configuration.GetSection("Resilience:MyRatePolicy");

        var services = new ServiceCollection();
        services.AddResiliencePolicyFromConfiguration("rate-policy", section);

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<IResiliencePipelineRegistry>();
        var pipeline = registry.GetPipeline("rate-policy");

        // 1st request consumes permit
        var res1 = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("Permitted"));
        res1.Should().Be("Permitted");

        // 2nd request exceeds permit limit
        Func<Task> rejectAct = async () =>
        {
            await pipeline.ExecuteAsync(ct => ValueTask.FromResult("ShouldReject"));
        };
        await rejectAct.Should().ThrowAsync<RateLimitRejectedException>();
    }

    [Fact]
    public async Task AddResiliencePolicyFromConfiguration_WithEmptySection_CompilesEmptyPipeline()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = configuration.GetSection("Resilience:Empty");

        var services = new ServiceCollection();
        services.AddResiliencePolicyFromConfiguration("empty-policy", section);

        var sp = services.BuildServiceProvider();
        var registry = sp.GetRequiredService<IResiliencePipelineRegistry>();
        var pipeline = registry.GetPipeline("empty-policy");

        var executed = await pipeline.ExecuteAsync(ct => ValueTask.FromResult("PassThrough"));
        executed.Should().Be("PassThrough");
    }

    #region Retry Options

    [Fact]
    public void BindRetryOptions_WithNullSection_ThrowsArgumentNullException()
    {
        IConfigurationSection? section = null;
        Action act = () => section!.BindRetryOptions();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BindRetryOptions_WithEmptySection_ReturnsDefaults()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var section = config.GetSection("Empty");

        var options = section.BindRetryOptions();

        options.MaxRetryAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromSeconds(2));
        options.MaxDelay.Should().BeNull();
        options.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        options.Name.Should().BeNull();
    }

    [Fact]
    public void BindRetryOptions_WithTimeSpanFormats_ParsesCorrectly()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Retry:MaxRetryAttempts", "5" },
            { "Retry:Delay", "00:00:03" },
            { "Retry:MaxDelay", "00:01:00" },
            { "Retry:BackoffType", "ExponentialWithJitter" },
            { "Retry:Name", "MyRetry" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = config.GetSection("Retry");

        var options = section.BindRetryOptions();

        options.MaxRetryAttempts.Should().Be(5);
        options.Delay.Should().Be(TimeSpan.FromSeconds(3));
        options.MaxDelay.Should().Be(TimeSpan.FromMinutes(1));
        options.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        options.Name.Should().Be("MyRetry");
    }

    [Theory]
    [InlineData("exponentialwithjitter", BackoffType.ExponentialWithJitter)]
    [InlineData("linear", BackoffType.Linear)]
    [InlineData("exponential", BackoffType.Exponential)]
    [InlineData("constant", BackoffType.Constant)]
    public void BindRetryOptions_WithCaseInsensitiveBackoffType_ParsesCorrectly(string backoffString, BackoffType expected)
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Retry:BackoffType", backoffString }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var options = config.GetSection("Retry").BindRetryOptions();

        options.BackoffType.Should().Be(expected);
    }

    [Fact]
    public void BindRetryOptions_WithSecondsAndMilliseconds_ParsesCorrectly()
    {
        // DelaySeconds and MaxDelaySeconds
        var settings1 = new Dictionary<string, string?>
        {
            { "Retry:DelaySeconds", "2.5" },
            { "Retry:MaxDelaySeconds", "15.0" },
            { "Retry:BackoffType", "Linear" },
            { "Retry:Name", "   " } // whitespace name ignored
        };
        var config1 = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        var options1 = config1.GetSection("Retry").BindRetryOptions();

        options1.Delay.Should().Be(TimeSpan.FromSeconds(2.5));
        options1.MaxDelay.Should().Be(TimeSpan.FromSeconds(15));
        options1.BackoffType.Should().Be(BackoffType.Linear);
        options1.Name.Should().BeNull();

        // DelayMilliseconds
        var settings2 = new Dictionary<string, string?>
        {
            { "Retry:DelayMilliseconds", "450" },
            { "Retry:BackoffType", "Exponential" }
        };
        var config2 = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();
        var options2 = config2.GetSection("Retry").BindRetryOptions();

        options2.Delay.Should().Be(TimeSpan.FromMilliseconds(450));
        options2.BackoffType.Should().Be(BackoffType.Exponential);
    }

    [Fact]
    public void BindRetryOptions_WithInvalidValues_RetainsDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Retry:MaxRetryAttempts", "invalid_int" },
            { "Retry:Delay", "invalid_timespan" },
            { "Retry:MaxDelay", "invalid_timespan" },
            { "Retry:BackoffType", "invalid_enum" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("Retry").BindRetryOptions();

        options.MaxRetryAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromSeconds(2));
        options.MaxDelay.Should().BeNull();
        options.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
    }

    #endregion

    #region CircuitBreaker Options

    [Fact]
    public void BindCircuitBreakerOptions_WithNullSection_ThrowsArgumentNullException()
    {
        IConfigurationSection? section = null;
        Action act = () => section!.BindCircuitBreakerOptions();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BindCircuitBreakerOptions_WithEmptySection_ReturnsDefaults()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var section = config.GetSection("Empty");

        var options = section.BindCircuitBreakerOptions();

        options.FailureRatio.Should().Be(0.5);
        options.MinimumThroughput.Should().Be(20);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(10));
        options.Name.Should().BeNull();
    }

    [Fact]
    public void BindCircuitBreakerOptions_WithTimeSpanFormats_ParsesCorrectly()
    {
        var settings = new Dictionary<string, string?>
        {
            { "CB:FailureRatio", "0.75" },
            { "CB:MinimumThroughput", "25" },
            { "CB:SamplingDuration", "00:00:45" },
            { "CB:BreakDuration", "00:00:20" },
            { "CB:Name", "CustomCB" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("CB").BindCircuitBreakerOptions();

        options.FailureRatio.Should().Be(0.75);
        options.MinimumThroughput.Should().Be(25);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(45));
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(20));
        options.Name.Should().Be("CustomCB");
    }

    [Fact]
    public void BindCircuitBreakerOptions_WithSecondsFormats_ParsesCorrectly()
    {
        var settings = new Dictionary<string, string?>
        {
            { "CB:SamplingDurationSeconds", "50" },
            { "CB:BreakDurationSeconds", "15" },
            { "CB:Name", "" } // empty name ignored
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("CB").BindCircuitBreakerOptions();

        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(50));
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(15));
        options.Name.Should().BeNull();
    }

    [Fact]
    public void BindCircuitBreakerOptions_WithInvalidValues_RetainsDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "CB:FailureRatio", "invalid_double" },
            { "CB:MinimumThroughput", "invalid_int" },
            { "CB:SamplingDuration", "invalid_timespan" },
            { "CB:BreakDuration", "invalid_timespan" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("CB").BindCircuitBreakerOptions();

        options.FailureRatio.Should().Be(0.5);
        options.MinimumThroughput.Should().Be(20);
        options.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        options.BreakDuration.Should().Be(TimeSpan.FromSeconds(10));
    }

    #endregion

    #region Timeout Options

    [Fact]
    public void BindTimeoutOptions_WithNullSection_ThrowsArgumentNullException()
    {
        IConfigurationSection? section = null;
        Action act = () => section!.BindTimeoutOptions();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BindTimeoutOptions_WithEmptySection_ReturnsDefaults()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var section = config.GetSection("Empty");

        var options = section.BindTimeoutOptions();

        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        options.Name.Should().BeNull();
    }

    [Fact]
    public void BindTimeoutOptions_WithTimeSpan_ParsesCorrectly()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Timeout:Timeout", "00:00:15" },
            { "Timeout:Name", "TimeoutA" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("Timeout").BindTimeoutOptions();

        options.Timeout.Should().Be(TimeSpan.FromSeconds(15));
        options.Name.Should().Be("TimeoutA");
    }

    [Fact]
    public void BindTimeoutOptions_WithSecondsAndMilliseconds_ParsesCorrectly()
    {
        // Seconds
        var settings1 = new Dictionary<string, string?>
        {
            { "Timeout:TimeoutSeconds", "7.5" },
            { "Timeout:Name", "   " }
        };
        var config1 = new ConfigurationBuilder().AddInMemoryCollection(settings1).Build();
        var options1 = config1.GetSection("Timeout").BindTimeoutOptions();
        options1.Timeout.Should().Be(TimeSpan.FromSeconds(7.5));
        options1.Name.Should().BeNull();

        // Milliseconds
        var settings2 = new Dictionary<string, string?>
        {
            { "Timeout:TimeoutMilliseconds", "350" }
        };
        var config2 = new ConfigurationBuilder().AddInMemoryCollection(settings2).Build();
        var options2 = config2.GetSection("Timeout").BindTimeoutOptions();
        options2.Timeout.Should().Be(TimeSpan.FromMilliseconds(350));
    }

    [Fact]
    public void BindTimeoutOptions_WithInvalidValues_RetainsDefaults()
    {
        var settings = new Dictionary<string, string?>
        {
            { "Timeout:Timeout", "invalid" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("Timeout").BindTimeoutOptions();

        options.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region RateLimiter Options

    [Fact]
    public void BindRateLimiterOptions_WithNullSection_ThrowsArgumentNullException()
    {
        IConfigurationSection? section = null;
        Action act = () => section!.BindRateLimiterOptions();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BindRateLimiterOptions_WithEmptySection_ReturnsDefaults()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var section = config.GetSection("Empty");

        var options = section.BindRateLimiterOptions();

        options.PermitLimit.Should().Be(100);
        options.QueueLimit.Should().Be(0);
        options.Window.Should().Be(TimeSpan.FromMinutes(1));
        options.LimiterType.Should().Be(RateLimiterType.SlidingWindow);
        options.Name.Should().BeNull();
    }

    [Fact]
    public void BindRateLimiterOptions_WithTimeSpan_ParsesCorrectly()
    {
        var settings = new Dictionary<string, string?>
        {
            { "RateLimiter:PermitLimit", "200" },
            { "RateLimiter:QueueLimit", "10" },
            { "RateLimiter:Window", "00:00:30" },
            { "RateLimiter:LimiterType", "FixedWindow" },
            { "RateLimiter:Name", "RateLimitA" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("RateLimiter").BindRateLimiterOptions();

        options.PermitLimit.Should().Be(200);
        options.QueueLimit.Should().Be(10);
        options.Window.Should().Be(TimeSpan.FromSeconds(30));
        options.LimiterType.Should().Be(RateLimiterType.FixedWindow);
        options.Name.Should().Be("RateLimitA");
    }

    [Theory]
    [InlineData("fixedwindow", RateLimiterType.FixedWindow)]
    [InlineData("slidingwindow", RateLimiterType.SlidingWindow)]
    [InlineData("tokenbucket", RateLimiterType.TokenBucket)]
    [InlineData("concurrency", RateLimiterType.Concurrency)]
    public void BindRateLimiterOptions_WithCaseInsensitiveLimiterType_ParsesCorrectly(string limiterString, RateLimiterType expected)
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "RateLimiter:LimiterType", limiterString }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var options = config.GetSection("RateLimiter").BindRateLimiterOptions();

        options.LimiterType.Should().Be(expected);
    }

    [Fact]
    public void BindRateLimiterOptions_WithWindowSeconds_ParsesCorrectly()
    {
        var settings = new Dictionary<string, string?>
        {
            { "RateLimiter:WindowSeconds", "45" },
            { "RateLimiter:LimiterType", "TokenBucket" },
            { "RateLimiter:Name", "   " }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("RateLimiter").BindRateLimiterOptions();

        options.Window.Should().Be(TimeSpan.FromSeconds(45));
        options.LimiterType.Should().Be(RateLimiterType.TokenBucket);
        options.Name.Should().BeNull();
    }

    [Fact]
    public void BindRateLimiterOptions_WithConcurrencyAndInvalidValues_ParsesCorrectly()
    {
        var settings = new Dictionary<string, string?>
        {
            { "RateLimiter:PermitLimit", "invalid" },
            { "RateLimiter:QueueLimit", "invalid" },
            { "RateLimiter:Window", "invalid" },
            { "RateLimiter:LimiterType", "invalid_enum" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var options = config.GetSection("RateLimiter").BindRateLimiterOptions();

        options.PermitLimit.Should().Be(100);
        options.QueueLimit.Should().Be(0);
        options.Window.Should().Be(TimeSpan.FromMinutes(1));
        options.LimiterType.Should().Be(RateLimiterType.SlidingWindow);
    }

    #endregion
}
