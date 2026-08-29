// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Provides context information when a retry attempt is triggered by a resilience strategy.
/// </summary>
public sealed class RetryAttemptContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryAttemptContext"/> class.
    /// </summary>
    /// <param name="resilienceContext">The active execution context.</param>
    /// <param name="attemptNumber">The zero-based or one-based attempt number that just failed.</param>
    /// <param name="delay">The computed delay before the next retry attempt.</param>
    /// <param name="exception">The exception that triggered the retry, if any.</param>
    /// <param name="result">The outcome result object that triggered the retry, if any.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resilienceContext"/> is <see langword="null"/></exception>
    public RetryAttemptContext(
        ResilienceContext resilienceContext,
        int attemptNumber,
        TimeSpan delay,
        Exception? exception = null,
        object? result = null)
    {
        ResilienceContext = resilienceContext ?? throw new ArgumentNullException(nameof(resilienceContext));
        AttemptNumber = attemptNumber;
        Delay = delay;
        Exception = exception;
        Result = result;
    }

    /// <summary>
    /// Gets the execution context associated with this retry attempt.
    /// </summary>
    public ResilienceContext ResilienceContext { get; }

    /// <summary>
    /// Gets the attempt index that triggered the retry.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the duration to wait before executing the next attempt.
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    /// Gets the exception that caused the failure, if an exception occurred.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the result object that caused the retry, if failure was determined by result evaluation.
    /// </summary>
    public object? Result { get; }
}
