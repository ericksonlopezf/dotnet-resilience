// Copyright © Erickson Lopez. MIT License.
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
    public const string MeterVersion = "2.0.0";

    private static readonly Meter _meter = new(MeterName, MeterVersion);

    private static readonly Histogram<double> _executionDuration = _meter.CreateHistogram<double>(
        "resilience.execution.duration",
        "ms",
        "Execution duration of resilient operations in milliseconds.");

    private static readonly Counter<long> _retryAttempts = _meter.CreateCounter<long>(
        "resilience.retry.attempts",
        "{attempt}",
        "Number of retry attempts executed.");

    private static readonly Counter<long> _circuitBreakerStateChanges = _meter.CreateCounter<long>(
        "resilience.circuit_breaker.state_changes",
        "{transition}",
        "Number of circuit breaker state transitions.");

    private static readonly Counter<long> _timeoutRejections = _meter.CreateCounter<long>(
        "resilience.timeout.rejections",
        "{rejection}",
        "Number of operations terminated due to timeout.");

    private static readonly Counter<long> _rateLimiterRejections = _meter.CreateCounter<long>(
        "resilience.rate_limiter.rejections",
        "{rejection}",
        "Number of operations rejected due to rate limiting.");

    private static readonly Counter<long> _circuitBreakerRejections = _meter.CreateCounter<long>(
        "resilience.circuit_breaker.rejections",
        "{rejection}",
        "Number of operations rejected due to open circuit breaker.");

    private const string _policyTagName = "resilience.policy";
    private const string _operationTagName = "resilience.operation";
    private const string _tenantIdTagName = "resilience.tenant_id";

    private static volatile bool _includeTenantIdTag;

    /// <summary>
    /// Gets or sets a value indicating whether tenant identifiers should be included in metric tags.
    /// Defaults to <see langword="false"/> to prevent metric cardinality explosion in multi-tenant environments.
    /// </summary>
    public static bool IncludeTenantIdTag
    {
        get => _includeTenantIdTag;
        set => _includeTenantIdTag = value;
    }

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
            { _policyTagName, policyName },
            { _operationTagName, operationName },
            { "resilience.status", isSuccess ? "success" : "failure" }
        };

        if (IncludeTenantIdTag && !string.IsNullOrEmpty(tenantId))
        {
            tags.Add(_tenantIdTagName, tenantId);
        }

        _executionDuration.Record(durationMs, tags);
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
            { _policyTagName, policyName },
            { _operationTagName, operationName },
            { "resilience.attempt", attemptNumber }
        };

        if (IncludeTenantIdTag && !string.IsNullOrEmpty(tenantId))
        {
            tags.Add(_tenantIdTagName, tenantId);
        }

        _retryAttempts.Add(1, tags);
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
            { _policyTagName, policyName },
            { "resilience.circuit.state", state.ToString() }
        };

        if (IncludeTenantIdTag && !string.IsNullOrEmpty(tenantId))
        {
            tags.Add(_tenantIdTagName, tenantId);
        }

        _circuitBreakerStateChanges.Add(1, tags);
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
            { _policyTagName, policyName },
            { _operationTagName, operationName }
        };

        if (IncludeTenantIdTag && !string.IsNullOrEmpty(tenantId))
        {
            tags.Add(_tenantIdTagName, tenantId);
        }

        _timeoutRejections.Add(1, tags);
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
            { _policyTagName, policyName },
            { _operationTagName, operationName }
        };

        if (IncludeTenantIdTag && !string.IsNullOrEmpty(tenantId))
        {
            tags.Add(_tenantIdTagName, tenantId);
        }

        _rateLimiterRejections.Add(1, tags);
    }

    /// <summary>
    /// Records a circuit breaker rejection due to open circuit state.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    public static void RecordCircuitBreakerRejection(
        string policyName,
        string operationName,
        string? tenantId = null)
    {
        var tags = new TagList
        {
            { _policyTagName, policyName },
            { _operationTagName, operationName }
        };

        if (IncludeTenantIdTag && !string.IsNullOrEmpty(tenantId))
        {
            tags.Add(_tenantIdTagName, tenantId);
        }

        _circuitBreakerRejections.Add(1, tags);
    }
}
