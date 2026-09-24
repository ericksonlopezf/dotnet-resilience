// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Specifies the type of rate limiting algorithm used by the rate limiter strategy.
/// </summary>
public enum RateLimiterType
{
    /// <summary>
    /// Divides the window into segments and smooths permit replenishment across sliding intervals.
    /// </summary>
    SlidingWindow = 0,

    /// <summary>
    /// Resets all permits at the start of each fixed time interval.
    /// </summary>
    FixedWindow = 1,

    /// <summary>
    /// Replenishes permits steadily into a token bucket up to a maximum capacity.
    /// </summary>
    TokenBucket = 2,

    /// <summary>
    /// Limits the maximum number of concurrent executions active simultaneously.
    /// </summary>
    Concurrency = 3
}
