// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Classification;

/// <summary>
/// Defines a contract for evaluating operation results and exceptions to classify their retryability.
/// </summary>
public interface IResultRetryClassifier
{
    /// <summary>
    /// Evaluates an outcome result object to determine if it represents a retryable transient failure.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The outcome result instance.</param>
    /// <returns>
    /// <see cref="RetryabilityDecision.Retry"/> if the result is a known transient failure that should be retried;
    /// <see cref="RetryabilityDecision.DoNotRetry"/> if the result represents a success or a permanent non-retryable failure;
    /// <see cref="RetryabilityDecision.Undetermined"/> if <typeparamref name="T"/> does not implement the <c>IResultOutcome</c>
    /// contract or the error cannot be classified with certainty, allowing the retry strategy to fall back to its exception-based predicate.
    /// </returns>
    RetryabilityDecision ClassifyResult<T>(T result);

    /// <summary>
    /// Evaluates an observed exception to determine if it represents a retryable transient failure.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>
    /// <see cref="RetryabilityDecision.Retry"/> if the exception is a known transient fault (e.g., timeout, socket, HTTP 503);
    /// <see cref="RetryabilityDecision.DoNotRetry"/> if the exception is deterministic or represents a permanent failure
    /// (e.g., <see cref="System.OperationCanceledException"/>, <see cref="System.ArgumentException"/>);
    /// <see cref="RetryabilityDecision.Undetermined"/> if the exception type is not recognized by this classifier.
    /// </returns>
    RetryabilityDecision ClassifyException(Exception exception);
}
