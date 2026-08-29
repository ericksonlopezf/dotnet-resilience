// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Provides context information when an execution is rejected by rate limiting constraints.
/// </summary>
public sealed class RateLimiterContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiterContext"/> class.
    /// </summary>
    /// <param name="resilienceContext">The active execution context.</param>
    /// <param name="retryAfter">The duration to wait before trying again, if calculated.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resilienceContext"/> is <see langword="null"/></exception>
    public RateLimiterContext(ResilienceContext resilienceContext, TimeSpan? retryAfter = null)
    {
        ResilienceContext = resilienceContext ?? throw new ArgumentNullException(nameof(resilienceContext));
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the execution context.
    /// </summary>
    public ResilienceContext ResilienceContext { get; }

    /// <summary>
    /// Gets the duration to wait before trying again, if provided by the rate limiter.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
