// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Classification;

/// <summary>
/// Specifies the deterministic classification outcome regarding whether an error or result is retryable.
/// </summary>
public enum RetryabilityDecision : byte
{
    /// <summary>
    /// Indicates that the classification could not be determined by the classifier.
    /// </summary>
    Undetermined = 0,

    /// <summary>
    /// Indicates that the failure is classified as transient and eligible for retry.
    /// </summary>
    Retry = 1,

    /// <summary>
    /// Indicates that the failure is classified as permanent or deterministic and must not be retried.
    /// </summary>
    DoNotRetry = 2
}
