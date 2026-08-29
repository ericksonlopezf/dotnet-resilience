// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.DependencyInjection;

/// <summary>
/// Provides reflection-free, Native AOT-safe configuration binding extension methods for mapping resilience options from <see cref="IConfiguration"/> and <see cref="IConfigurationSection"/>.
/// </summary>
public static class ResilienceConfigurationExtensions
{
    /// <summary>
    /// Configures a named resilience policy by reading its strategy settings from the specified <see cref="IConfigurationSection"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policyName">The unique policy name.</param>
    /// <param name="section">The configuration section containing resilience parameters (e.g. Retry, CircuitBreaker, Timeout, RateLimiter).</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="section"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public static IServiceCollection AddResiliencePolicyFromConfiguration(
        this IServiceCollection services,
        string policyName,
        IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return services.AddResiliencePolicy(policyName, builder =>
        {
            var timeoutSection = section.GetSection("Timeout");
            if (timeoutSection.Exists())
            {
                builder.AddTimeout(timeoutSection.BindTimeoutOptions());
            }

            var retrySection = section.GetSection("Retry");
            if (retrySection.Exists())
            {
                builder.AddRetry(retrySection.BindRetryOptions());
            }

            var cbSection = section.GetSection("CircuitBreaker");
            if (cbSection.Exists())
            {
                builder.AddCircuitBreaker(cbSection.BindCircuitBreakerOptions());
            }

            var rateSection = section.GetSection("RateLimiter");
            if (rateSection.Exists())
            {
                builder.AddRateLimiter(rateSection.BindRateLimiterOptions());
            }
        });
    }

    /// <summary>
    /// Binds <see cref="RetryStrategyOptions"/> from the given configuration section without using reflection.
    /// </summary>
    /// <param name="section">The configuration section.</param>
    /// <returns>A populated <see cref="RetryStrategyOptions"/> instance.</returns>
    public static RetryStrategyOptions BindRetryOptions(this IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var options = new RetryStrategyOptions();

        if (int.TryParse(section["MaxRetryAttempts"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxAttempts))
        {
            options.MaxRetryAttempts = maxAttempts;
        }

        if (TimeSpan.TryParse(section["Delay"], CultureInfo.InvariantCulture, out var delay))
        {
            options.Delay = delay;
        }
        else if (double.TryParse(section["DelaySeconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var delaySec))
        {
            options.Delay = TimeSpan.FromSeconds(delaySec);
        }
        else if (double.TryParse(section["DelayMilliseconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var delayMs))
        {
            options.Delay = TimeSpan.FromMilliseconds(delayMs);
        }

        if (TimeSpan.TryParse(section["MaxDelay"], CultureInfo.InvariantCulture, out var maxDelay))
        {
            options.MaxDelay = maxDelay;
        }
        else if (double.TryParse(section["MaxDelaySeconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var maxDelaySec))
        {
            options.MaxDelay = TimeSpan.FromSeconds(maxDelaySec);
        }

        var backoffTypeValue = section["BackoffType"];
        if (!string.IsNullOrWhiteSpace(backoffTypeValue))
        {
            options.BackoffType = backoffTypeValue.Trim().ToUpperInvariant() switch
            {
                "CONSTANT" => BackoffType.Constant,
                "LINEAR" => BackoffType.Linear,
                "EXPONENTIAL" => BackoffType.Exponential,
                "EXPONENTIALWITHJITTER" => BackoffType.ExponentialWithJitter,
                _ => options.BackoffType // retain default on unrecognized value
            };
        }

        if (!string.IsNullOrWhiteSpace(section["Name"]))
        {
            options.Name = section["Name"];
        }

        return options;
    }

    /// <summary>
    /// Binds <see cref="CircuitBreakerStrategyOptions"/> from the given configuration section without using reflection.
    /// </summary>
    /// <param name="section">The configuration section.</param>
    /// <returns>A populated <see cref="CircuitBreakerStrategyOptions"/> instance.</returns>
    public static CircuitBreakerStrategyOptions BindCircuitBreakerOptions(this IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var options = new CircuitBreakerStrategyOptions();

        if (double.TryParse(section["FailureRatio"], NumberStyles.Float, CultureInfo.InvariantCulture, out var failureRatio))
        {
            options.FailureRatio = failureRatio;
        }

        if (int.TryParse(section["MinimumThroughput"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minThroughput))
        {
            options.MinimumThroughput = minThroughput;
        }

        if (TimeSpan.TryParse(section["SamplingDuration"], CultureInfo.InvariantCulture, out var samplingDuration))
        {
            options.SamplingDuration = samplingDuration;
        }
        else if (double.TryParse(section["SamplingDurationSeconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var samplingSec))
        {
            options.SamplingDuration = TimeSpan.FromSeconds(samplingSec);
        }

        if (TimeSpan.TryParse(section["BreakDuration"], CultureInfo.InvariantCulture, out var breakDuration))
        {
            options.BreakDuration = breakDuration;
        }
        else if (double.TryParse(section["BreakDurationSeconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var breakSec))
        {
            options.BreakDuration = TimeSpan.FromSeconds(breakSec);
        }

        if (!string.IsNullOrWhiteSpace(section["Name"]))
        {
            options.Name = section["Name"];
        }

        return options;
    }

    /// <summary>
    /// Binds <see cref="TimeoutStrategyOptions"/> from the given configuration section without using reflection.
    /// </summary>
    /// <param name="section">The configuration section.</param>
    /// <returns>A populated <see cref="TimeoutStrategyOptions"/> instance.</returns>
    public static TimeoutStrategyOptions BindTimeoutOptions(this IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var options = new TimeoutStrategyOptions();

        if (TimeSpan.TryParse(section["Timeout"], CultureInfo.InvariantCulture, out var timeout))
        {
            options.Timeout = timeout;
        }
        else if (double.TryParse(section["TimeoutSeconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutSec))
        {
            options.Timeout = TimeSpan.FromSeconds(timeoutSec);
        }
        else if (double.TryParse(section["TimeoutMilliseconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutMs))
        {
            options.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        }

        if (!string.IsNullOrWhiteSpace(section["Name"]))
        {
            options.Name = section["Name"];
        }

        return options;
    }

    /// <summary>
    /// Binds <see cref="RateLimiterStrategyOptions"/> from the given configuration section without using reflection.
    /// </summary>
    /// <param name="section">The configuration section.</param>
    /// <returns>A populated <see cref="RateLimiterStrategyOptions"/> instance.</returns>
    public static RateLimiterStrategyOptions BindRateLimiterOptions(this IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        var options = new RateLimiterStrategyOptions();

        if (int.TryParse(section["PermitLimit"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var permitLimit))
        {
            options.PermitLimit = permitLimit;
        }

        if (int.TryParse(section["QueueLimit"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var queueLimit))
        {
            options.QueueLimit = queueLimit;
        }

        if (TimeSpan.TryParse(section["Window"], CultureInfo.InvariantCulture, out var window))
        {
            options.Window = window;
        }
        else if (double.TryParse(section["WindowSeconds"], NumberStyles.Float, CultureInfo.InvariantCulture, out var windowSec))
        {
            options.Window = TimeSpan.FromSeconds(windowSec);
        }

        var limiterTypeValue = section["LimiterType"];
        if (!string.IsNullOrWhiteSpace(limiterTypeValue))
        {
            options.LimiterType = limiterTypeValue.Trim().ToUpperInvariant() switch
            {
                "SLIDINGWINDOW" => RateLimiterType.SlidingWindow,
                "FIXEDWINDOW" => RateLimiterType.FixedWindow,
                "TOKENBUCKET" => RateLimiterType.TokenBucket,
                "CONCURRENCY" => RateLimiterType.Concurrency,
                _ => options.LimiterType // retain default on unrecognized value
            };
        }

        if (!string.IsNullOrWhiteSpace(section["Name"]))
        {
            options.Name = section["Name"];
        }

        return options;
    }
}
