// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Classification;

/// <summary>
/// Defines a contract for evaluating domain or infrastructure errors to determine their retryability.
/// </summary>
public interface IErrorClassifier
{
    /// <summary>
    /// Evaluates an error object to determine if it represents a retryable transient failure.
    /// </summary>
    /// <param name="errorInstance">The error object (e.g., error outcome, exception, or status).</param>
    /// <returns>A <see cref="RetryabilityDecision"/> indicating whether the error represents a retryable transient failure.</returns>
    RetryabilityDecision ClassifyError(object? errorInstance);
}
