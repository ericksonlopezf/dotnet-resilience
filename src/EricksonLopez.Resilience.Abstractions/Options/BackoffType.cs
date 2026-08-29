// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Specifies the algorithm used to calculate backoff delays between retry attempts.
/// </summary>
public enum BackoffType : byte
{
    /// <summary>
    /// Indicates that delays between retries are constant (fixed interval).
    /// </summary>
    Constant = 0,

    /// <summary>
    /// Indicates that delays between retries increase linearly with each attempt.
    /// </summary>
    Linear = 1,

    /// <summary>
    /// Indicates that delays between retries increase exponentially (2^attempt * base delay).
    /// </summary>
    Exponential = 2,

    /// <summary>
    /// Indicates that delays between retries increase exponentially with full jitter applied to prevent thundering herd problems.
    /// </summary>
    ExponentialWithJitter = 3
}
