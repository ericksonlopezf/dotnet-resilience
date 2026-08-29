// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics;

namespace EricksonLopez.Resilience.OpenTelemetry;

/// <summary>
/// Provides OpenTelemetry activity source tracing instrumentation for the EricksonLopez resilience ecosystem.
/// </summary>
public static class ResilienceActivitySource
{
    /// <summary>
    /// Gets the name of the OpenTelemetry activity source.
    /// </summary>
    public const string SourceName = "EricksonLopez.Resilience";

    /// <summary>
    /// Gets the version of the OpenTelemetry activity source.
    /// </summary>
    public const string SourceVersion = "1.0.0";

    private static readonly ActivitySource Source = new(SourceName, SourceVersion);

    /// <summary>
    /// Starts a new tracing activity for a resilient operation execution.
    /// </summary>
    /// <param name="policyName">The policy name.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    /// <param name="correlationId">The optional correlation identifier.</param>
    /// <returns>A new <see cref="Activity"/> instance, or <see langword="null"/> if tracing is not enabled.</returns>
    public static Activity? StartExecutionActivity(
        string policyName,
        string operationName,
        string? tenantId = null,
        string? correlationId = null)
    {
        var activity = Source.StartActivity("Resilience.Execute", ActivityKind.Internal);
        if (activity != null)
        {
            activity.SetTag("resilience.policy", policyName);
            activity.SetTag("resilience.operation", operationName);

            if (!string.IsNullOrEmpty(tenantId))
            {
                activity.SetTag("resilience.tenant_id", tenantId);
            }

            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.SetTag("resilience.correlation_id", correlationId);
            }
        }

        return activity;
    }
}
