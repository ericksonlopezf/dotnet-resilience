// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Classification;

/// <summary>
/// Provides utility methods for identifying transient exceptions in .NET runtime and infrastructure components.
/// </summary>
public static class TransientExceptionClassifier
{
    /// <summary>
    /// Determines whether the specified exception represents a transient failure eligible for retry.
    /// </summary>
    /// <param name="exception">The exception to evaluate.</param>
    /// <returns><see langword="true"/> if the exception is transient; otherwise, <see langword="false"/>.</returns>
    public static bool IsTransient(Exception exception)
    {
        return ResultRetryClassifier.Instance.ClassifyException(exception) == RetryabilityDecision.Retry;
    }
}
