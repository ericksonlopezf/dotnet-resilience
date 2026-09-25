// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using EricksonLopez.Result;

namespace EricksonLopez.Resilience.Classification;

/// <summary>
/// Evaluates <see cref="Result{T}"/>, <see cref="Result"/>, and domain <see cref="Error"/> objects
/// alongside runtime exceptions to make deterministic retry decisions within the EricksonLopez ecosystem.
/// </summary>
public sealed class ResultRetryClassifier : IResultRetryClassifier, IErrorClassifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultRetryClassifier"/> class.
    /// </summary>
    public ResultRetryClassifier()
    {
    }

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

        // Framework timeout or Polly TimeoutRejectedException is transient and retryable if configured
        if (exception is Exceptions.ResilienceTimeoutException or TimeoutException
            || exception.GetType().Name == "TimeoutRejectedException")
        {
            return RetryabilityDecision.Retry;
        }

        // Caller cancellation must never be retried
        if (exception is OperationCanceledException)
        {
            return RetryabilityDecision.DoNotRetry;
        }

        // Database deadlocks and transient connection drops (SQL Server 1205, Postgres 40P01, MySQL 1213)
        if (IsDatabaseDeadlockOrTransient(exception))
        {
            return RetryabilityDecision.Retry;
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

    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection on optional database exception properties Number and SqlState is best-effort and safe.")]
    private static bool IsDatabaseDeadlockOrTransient(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            var typeName = current.GetType().Name;
            if (typeName == "SqlException")
            {
                var numberProp = current.GetType().GetProperty("Number");
                if (numberProp != null && numberProp.GetValue(current) is int number)
                {
                    if (number is 1205 or 3960 or 10053 or 10054 or 10060 or 40613 or 40197 or 40501)
                    {
                        return true;
                    }
                }
            }
            else if (typeName is "NpgsqlException" or "PostgresException")
            {
                var sqlStateProp = current.GetType().GetProperty("SqlState");
                if (sqlStateProp != null && sqlStateProp.GetValue(current) is string sqlState)
                {
                    if (sqlState is "40P01" or "40001" or "08000" or "08003" or "08006" or "57P01")
                    {
                        return true;
                    }
                }
            }
            else if (typeName == "MySqlException")
            {
                var numberProp = current.GetType().GetProperty("Number");
                if (numberProp != null && numberProp.GetValue(current) is int number)
                {
                    if (number is 1213 or 1205)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
