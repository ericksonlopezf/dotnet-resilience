// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for the rate limiter resilience strategy.
/// </summary>
public sealed class RateLimiterStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// Gets or sets the maximum number of permits allowed within the given window.
    /// Default is 100 permits.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of requests that can be queued waiting for a permit.
    /// Default is 0 (no queueing; immediate rejection if limit exceeded).
    /// </summary>
    public int QueueLimit { get; set; }

    /// <summary>
    /// Gets or sets the replenishment time window.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the rate limiter algorithm type.
    /// Default is <see cref="RateLimiterType.SlidingWindow"/>.
    /// </summary>
    public RateLimiterType LimiterType { get; set; } = RateLimiterType.SlidingWindow;

    /// <summary>
    /// Gets or sets a custom <see cref="System.Threading.RateLimiting.RateLimiter"/> instance.
    /// When provided, this limiter overrides <see cref="LimiterType"/> and other parameters.
    /// </summary>
    public System.Threading.RateLimiting.RateLimiter? CustomRateLimiter { get; set; }

    /// <summary>
    /// Gets or sets an asynchronous callback invoked when an execution is rejected by the rate limiter.
    /// </summary>
    public Func<RateLimiterContext, ValueTask>? OnRejected { get; set; }
}
