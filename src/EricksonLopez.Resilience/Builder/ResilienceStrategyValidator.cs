// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;

namespace EricksonLopez.Resilience.Builder;

internal static class ResilienceStrategyValidator
{
    public static void ValidateRetryOptions(RetryStrategyOptions options)
    {
        if (options.MaxRetryAttempts < 0)
        {
            throw new ResilienceConfigurationException("Retry MaxRetryAttempts must be greater than or equal to 0.");
        }

        if (options.Delay < TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Retry Delay must be greater than or equal to TimeSpan.Zero.");
        }

        if (options.MaxDelay.HasValue && options.MaxDelay.Value < options.Delay)
        {
            throw new ResilienceConfigurationException("Retry MaxDelay cannot be less than base Delay.");
        }
    }

    public static void ValidateCircuitBreakerOptions(CircuitBreakerStrategyOptions options)
    {
        if (options.FailureRatio is <= 0.0 or > 1.0)
        {
            throw new ResilienceConfigurationException("CircuitBreaker FailureRatio must be greater than 0.0 and less than or equal to 1.0.");
        }

        if (options.MinimumThroughput <= 0)
        {
            throw new ResilienceConfigurationException("CircuitBreaker MinimumThroughput must be greater than 0.");
        }

        if (options.SamplingDuration <= TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("CircuitBreaker SamplingDuration must be greater than TimeSpan.Zero.");
        }

        if (options.BreakDuration <= TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("CircuitBreaker BreakDuration must be greater than TimeSpan.Zero.");
        }
    }

    public static void ValidateTimeoutOptions(TimeoutStrategyOptions options)
    {
        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Timeout must be greater than TimeSpan.Zero.");
        }
    }

    public static void ValidateRateLimiterOptions(RateLimiterStrategyOptions options)
    {
        if (options.CustomRateLimiter == null)
        {
            if (options.PermitLimit <= 0)
            {
                throw new ResilienceConfigurationException("RateLimiter PermitLimit must be greater than 0.");
            }

            if (options.QueueLimit < 0)
            {
                throw new ResilienceConfigurationException("RateLimiter QueueLimit cannot be negative.");
            }

            if (options.Window <= TimeSpan.Zero)
            {
                throw new ResilienceConfigurationException("RateLimiter Window must be greater than TimeSpan.Zero.");
            }
        }
    }

    public static void ValidateHedgingOptions(HedgingStrategyOptions options)
    {
        if (options.MaxHedgedAttempts <= 0)
        {
            throw new ResilienceConfigurationException("Hedging MaxHedgedAttempts must be greater than 0.");
        }

        if (options.Delay < TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Hedging Delay cannot be negative.");
        }
    }

    public static void ValidateHedgingOptions<TResult>(HedgingStrategyOptions<TResult> options)
    {
        if (options.MaxHedgedAttempts <= 0)
        {
            throw new ResilienceConfigurationException("Hedging MaxHedgedAttempts must be greater than 0.");
        }

        if (options.Delay < TimeSpan.Zero)
        {
            throw new ResilienceConfigurationException("Hedging Delay cannot be negative.");
        }
    }
}
