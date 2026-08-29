// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Exceptions;

/// <summary>
/// Represents the exception thrown when an execution is rejected because the circuit breaker is currently in the Open state.
/// </summary>
public sealed class CircuitBrokenException : ResilienceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBrokenException"/> class.
    /// </summary>
    /// <param name="policyName">The policy name associated with the open circuit.</param>
    /// <param name="retryAfter">The duration remaining before the circuit transitions to half-open, if available.</param>
    public CircuitBrokenException(string policyName, TimeSpan? retryAfter = null)
        : base($"Execution rejected because the circuit breaker for policy '{policyName}' is currently Open." +
               (retryAfter.HasValue ? $" Try again after {retryAfter.Value.TotalSeconds:F1}s." : string.Empty))
    {
        PolicyName = policyName;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBrokenException"/> class with an inner exception.
    /// </summary>
    /// <param name="policyName">The policy name associated with the open circuit.</param>
    /// <param name="retryAfter">The duration remaining before the circuit transitions to half-open, if available.</param>
    /// <param name="innerException">The inner exception that originally opened the circuit.</param>
    public CircuitBrokenException(string policyName, TimeSpan? retryAfter, Exception innerException)
        : base($"Execution rejected because the circuit breaker for policy '{policyName}' is currently Open." +
               (retryAfter.HasValue ? $" Try again after {retryAfter.Value.TotalSeconds:F1}s." : string.Empty), innerException)
    {
        PolicyName = policyName;
        RetryAfter = retryAfter;
    }

    /// <summary>
    /// Gets the name of the policy whose circuit is open.
    /// </summary>
    public string PolicyName { get; }

    /// <summary>
    /// Gets the optional retry-after duration remaining on the break.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}
