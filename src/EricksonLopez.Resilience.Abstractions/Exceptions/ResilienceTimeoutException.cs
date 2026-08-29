// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Exceptions;

/// <summary>
/// Represents the exception thrown when an operation execution exceeds the configured resilience timeout duration.
/// </summary>
public sealed class ResilienceTimeoutException : ResilienceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTimeoutException"/> class.
    /// </summary>
    /// <param name="timeout">The configured timeout duration that was exceeded.</param>
    /// <param name="policyName">The optional policy name under which timeout occurred.</param>
    public ResilienceTimeoutException(TimeSpan timeout, string? policyName = null)
        : base($"Operation execution timed out after {timeout.TotalMilliseconds}ms in policy '{policyName ?? "Unknown"}'.")
    {
        Timeout = timeout;
        PolicyName = policyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTimeoutException"/> class with an inner exception.
    /// </summary>
    /// <param name="timeout">The configured timeout duration that was exceeded.</param>
    /// <param name="policyName">The optional policy name under which timeout occurred.</param>
    /// <param name="innerException">The inner exception associated with the timeout.</param>
    public ResilienceTimeoutException(TimeSpan timeout, string? policyName, Exception innerException)
        : base($"Operation execution timed out after {timeout.TotalMilliseconds}ms in policy '{policyName ?? "Unknown"}'.", innerException)
    {
        Timeout = timeout;
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the optional policy name under which timeout occurred.
    /// </summary>
    public string? PolicyName { get; }
}
