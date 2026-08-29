// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Exceptions;

/// <summary>
/// Represents the exception thrown when an operation execution is rejected due to active rate limiting constraints.
/// </summary>
public sealed class RateLimitRejectedException : ResilienceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitRejectedException"/> class.
    /// </summary>
    /// <param name="policyName">The policy name enforcing rate limiting.</param>
    /// <param name="retryAfter">The duration to wait before retrying the operation, if available.</param>
    public RateLimitRejectedException(string policyName, TimeSpan? retryAfter = null)
        : base($"Execution rejected by rate limiter in policy '{policyName}'." +
               (retryAfter.HasValue ? $" Retry after {retryAfter.Value.TotalSeconds:F1}s." : string.Empty))
    {
        PolicyName = policyName;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitRejectedException"/> class with an inner exception.
    /// </summary>
    /// <param name="policyName">The policy name enforcing rate limiting.</param>
    /// <param name="retryAfter">The duration to wait before retrying the operation, if available.</param>
    /// <param name="innerException">The inner exception associated with the rejection.</param>
    public RateLimitRejectedException(string policyName, TimeSpan? retryAfter, Exception innerException)
        : base($"Execution rejected by rate limiter in policy '{policyName}'." +
               (retryAfter.HasValue ? $" Retry after {retryAfter.Value.TotalSeconds:F1}s." : string.Empty), innerException)
    {
        PolicyName = policyName;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the name of the policy that rejected the execution.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Gets the optional retry-after duration suggested by the rate limiter.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
