// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Result;

namespace EricksonLopez.Resilience.Classification;

/// <summary>
/// Evaluates <see cref="Result{T}"/>, <see cref="Result"/>, and domain <see cref="Error"/> objects
/// alongside runtime exceptions to make deterministic retry decisions within the EricksonLopez ecosystem.
/// </summary>
public sealed class ResultRetryClassifier : IResultRetryClassifier, IErrorClassifier
{
    /// <summary>
    /// Gets the shared singleton instance of the default <see cref="ResultRetryClassifier"/>.
    /// </summary>
    public static ResultRetryClassifier Instance { get; } = new();

    /// <inheritdoc/>
    public RetryabilityDecision ClassifyResult<T>(T result)
    {
        if (result is IResultOutcome outcome)
        {
            if (outcome.IsSuccess)
            {
                return RetryabilityDecision.DoNotRetry;
            }

            if (outcome.Error is { } error)
            {
                return ClassifyError(error);
            }
        }

        return RetryabilityDecision.Undetermined;
    }

    /// <inheritdoc/>
    public RetryabilityDecision ClassifyError(object? errorInstance)
    {
        if (errorInstance is null)
        {
            return RetryabilityDecision.DoNotRetry;
        }

        if (errorInstance is Error error)
        {
            // Explicit error retryability takes absolute precedence
            if (error.Retryability == ErrorRetryability.Transient)
            {
                return RetryabilityDecision.Retry;
            }

            if (error.Retryability == ErrorRetryability.Permanent)
            {
                return RetryabilityDecision.DoNotRetry;
            }

            // Fall back to category-based classification
            return error.Type switch
            {
                ErrorType.Unavailable or ErrorType.Infrastructure => RetryabilityDecision.Retry,
                ErrorType.Validation or ErrorType.Domain or ErrorType.Unauthorized
                    or ErrorType.Forbidden or ErrorType.NotFound or ErrorType.Conflict => RetryabilityDecision.DoNotRetry,
                _ => RetryabilityDecision.Undetermined
            };
        }

        return RetryabilityDecision.Undetermined;
    }

    /// <inheritdoc/>
    public RetryabilityDecision ClassifyException(Exception exception)
    {
        if (exception is null)
        {
            return RetryabilityDecision.DoNotRetry;
        }

        // Framework timeout is transient and retryable if configured
        if (exception is Exceptions.ResilienceTimeoutException or TimeoutException)
        {
            return RetryabilityDecision.Retry;
        }

        // Caller cancellation must never be retried
        if (exception is OperationCanceledException)
        {
            return RetryabilityDecision.DoNotRetry;
        }

        // Socket and network I/O failures are transient
        if (exception is SocketException or IOException)
        {
            return RetryabilityDecision.Retry;
        }

        // HTTP network exceptions with transient status codes
        if (exception is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode.HasValue)
            {
                var code = (int)httpEx.StatusCode.Value;
                if (code is 408 or 429 or 500 or 502 or 503 or 504)
                {
                    return RetryabilityDecision.Retry;
                }

                return RetryabilityDecision.DoNotRetry;
            }

            return RetryabilityDecision.Retry;
        }

        // Deterministic contract/validation exceptions
        if (exception is ArgumentException or InvalidOperationException or NotSupportedException)
        {
            return RetryabilityDecision.DoNotRetry;
        }

        return RetryabilityDecision.Undetermined;
    }
}
