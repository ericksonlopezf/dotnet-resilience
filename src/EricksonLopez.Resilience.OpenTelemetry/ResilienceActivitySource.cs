// Copyright © Erickson Lopez. MIT License.
using System;
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
    public const string SourceVersion = "2.0.0";

    private static readonly ActivitySource _source = new(SourceName, SourceVersion);

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
        var activity = _source.StartActivity("Resilience.Execute", ActivityKind.Internal);
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

    /// <summary>
    /// Gets or sets an optional delegate to redact sensitive information from recorded exception messages and stack traces.
    /// </summary>
    public static Func<Exception, (string Message, string StackTrace)>? ExceptionSanitizer { get; set; }

    /// <summary>
    /// Records an exception event on the specified activity following OpenTelemetry semantic conventions.
    /// </summary>
    /// <param name="activity">The active activity.</param>
    /// <param name="exception">The exception to record.</param>
    public static void RecordException(Activity? activity, Exception exception)
    {
        if (activity == null || exception == null)
        {
            return;
        }

        var message = exception.Message;
        var stackTrace = exception.ToString();

        if (ExceptionSanitizer != null)
        {
            var sanitized = ExceptionSanitizer(exception);
            message = sanitized.Message;
            stackTrace = sanitized.StackTrace;
        }

        activity.SetStatus(ActivityStatusCode.Error, message);

        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName ?? exception.GetType().Name },
            { "exception.message", message },
            { "exception.stacktrace", stackTrace }
        };

        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }
}
