// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using EricksonLopez.Resilience.Options;

namespace EricksonLopez.Resilience.OpenTelemetry;

/// <summary>
/// Provides OpenTelemetry metrics instrumentation for the EricksonLopez resilience ecosystem.
/// </summary>
public static class ResilienceMeter
{
    /// <summary>
    /// Gets the name of the OpenTelemetry meter.
    /// </summary>
    public const string MeterName = "EricksonLopez.Resilience";

    /// <summary>
    /// Gets the version of the OpenTelemetry meter.
    /// </summary>
    public const string MeterVersion = "1.0.0";

    private static readonly Meter Meter = new(MeterName, MeterVersion);

    private static readonly Histogram<double> ExecutionDuration = Meter.CreateHistogram<double>(
        "resilience.execution.duration",
        "ms",
        "Execution duration of resilient operations in milliseconds.");

    private static readonly Counter<long> RetryAttempts = Meter.CreateCounter<long>(
        "resilience.retry.attempts",
        "{attempt}",
        "Number of retry attempts executed.");

    private static readonly Counter<long> CircuitBreakerStateChanges = Meter.CreateCounter<long>(
        "resilience.circuit_breaker.state_changes",
        "{transition}",
        "Number of circuit breaker state transitions.");

    private static readonly Counter<long> TimeoutRejections = Meter.CreateCounter<long>(
        "resilience.timeout.rejections",
        "{rejection}",
        "Number of operations terminated due to timeout.");

    private static readonly Counter<long> RateLimiterRejections = Meter.CreateCounter<long>(
        "resilience.rate_limiter.rejections",
        "{rejection}",
        "Number of operations rejected due to rate limiting.");

    private const string PolicyTagName = "resilience.policy";
    private const string OperationTagName = "resilience.operation";
    private const string TenantIdTagName = "resilience.tenant_id";

    /// <summary>
    /// Records the execution duration and outcome of a resilient operation.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="durationMs">The execution duration in milliseconds.</param>
    /// <param name="isSuccess">A value indicating whether the execution succeeded.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    public static void RecordExecution(
        string policyName,
        string operationName,
        double durationMs,
        bool isSuccess,
        string? tenantId = null)
    {
        var tags = new TagList
        {
            { PolicyTagName, policyName },
            { OperationTagName, operationName },
            { "resilience.status", isSuccess ? "success" : "failure" }
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add(TenantIdTagName, tenantId);
        }

        ExecutionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a retry attempt for a given policy and operation.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    public static void RecordRetry(
        string policyName,
        string operationName,
        int attemptNumber,
        string? tenantId = null)
    {
        var tags = new TagList
        {
            { PolicyTagName, policyName },
            { OperationTagName, operationName },
            { "resilience.attempt", attemptNumber }
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add(TenantIdTagName, tenantId);
        }

        RetryAttempts.Add(1, tags);
    }

    /// <summary>
    /// Records a circuit breaker state transition.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="state">The new circuit breaker state.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    public static void RecordCircuitStateChange(
        string policyName,
        CircuitBreakerState state,
        string? tenantId = null)
    {
        var tags = new TagList
        {
            { PolicyTagName, policyName },
            { "resilience.circuit.state", state.ToString() }
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add(TenantIdTagName, tenantId);
        }

        CircuitBreakerStateChanges.Add(1, tags);
    }

    /// <summary>
    /// Records a timeout rejection.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    public static void RecordTimeout(
        string policyName,
        string operationName,
        string? tenantId = null)
    {
        var tags = new TagList
        {
            { PolicyTagName, policyName },
            { OperationTagName, operationName }
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add(TenantIdTagName, tenantId);
        }

        TimeoutRejections.Add(1, tags);
    }

    /// <summary>
    /// Records a rate limiter rejection.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    public static void RecordRateLimitRejection(
        string policyName,
        string operationName,
        string? tenantId = null)
    {
        var tags = new TagList
        {
            { PolicyTagName, policyName },
            { OperationTagName, operationName }
        };

        if (!string.IsNullOrEmpty(tenantId))
        {
            tags.Add(TenantIdTagName, tenantId);
        }

        RateLimiterRejections.Add(1, tags);
    }
}
