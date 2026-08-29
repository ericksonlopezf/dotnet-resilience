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
    /// <returns>A <see cref="RetryabilityDecision"/> indicating whether the result represents a retryable transient failure.</returns>
    RetryabilityDecision ClassifyResult<T>(T result);

    /// <summary>
    /// Evaluates an observed exception to determine if it represents a retryable transient failure.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>A <see cref="RetryabilityDecision"/> indicating whether the exception represents a retryable transient failure.</returns>
    RetryabilityDecision ClassifyException(Exception exception);
}
