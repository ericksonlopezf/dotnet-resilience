// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.OpenTelemetry;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Resilience.Polly.Adapters;

/// <summary>
/// Provides an <see cref="IResilienceExecutor"/> implementation that resolves configured resilience pipelines from <see cref="IResiliencePipelineRegistry"/>,
/// dispatches executions through Polly-backed pipelines, and records OpenTelemetry telemetry and structured logging.
/// </summary>
public sealed class PollyResilienceExecutor : IResilienceExecutor
{
    private static readonly Action<ILogger, string, string, string, string, Exception?> _logExecutingTrace =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Trace,
            new EventId(1, "ResilienceExecuting"),
            "Executing resilient operation {OperationName} with policy {PolicyName} (Tenant: {TenantId}, Correlation: {CorrelationId})");

    private static readonly Action<ILogger, string, string, double, Exception?> _logSuccessDebug =
        LoggerMessage.Define<string, string, double>(
            LogLevel.Debug,
            new EventId(2, "ResilienceSuccess"),
            "Resilient operation {OperationName} with policy {PolicyName} succeeded in {ElapsedMs:F2}ms");

    private static readonly Action<ILogger, string, string, TimeSpan, double, Exception?> _logTimeoutWarning =
        LoggerMessage.Define<string, string, TimeSpan, double>(
            LogLevel.Warning,
            new EventId(3, "ResilienceTimeout"),
            "Resilient operation {OperationName} with policy {PolicyName} timed out after {TimeoutDuration} (Elapsed: {ElapsedMs:F2}ms)");

    private static readonly Action<ILogger, string, string, string, Exception?> _logCircuitBrokenWarning =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(4, "ResilienceCircuitBroken"),
            "Resilient operation {OperationName} blocked because circuit breaker {PolicyName} is open (RetryAfter: {RetryAfter})");

    private static readonly Action<ILogger, string, string, string, Exception?> _logRateLimitWarning =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(5, "ResilienceRateLimited"),
            "Resilient operation {OperationName} rejected by rate limiter for policy {PolicyName} (RetryAfter: {RetryAfter})");

    private static readonly Action<ILogger, string, string, double, Exception?> _logFailedError =
        LoggerMessage.Define<string, string, double>(
            LogLevel.Error,
            new EventId(6, "ResilienceFailed"),
            "Resilient operation {OperationName} with policy {PolicyName} failed after {ElapsedMs:F2}ms");

    private readonly IResiliencePipelineRegistry _registry;
    private readonly ILogger<PollyResilienceExecutor>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollyResilienceExecutor"/> class.
    /// </summary>
    /// <param name="registry">The resilience pipeline registry used to resolve pipelines by name.</param>
    /// <param name="logger">An optional structured logger instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="registry"/> is <see langword="null"/></exception>
    public PollyResilienceExecutor(
        IResiliencePipelineRegistry registry,
        ILogger<PollyResilienceExecutor>? logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger;
    }

    /// <inheritdoc/>
    public async ValueTask<TResult> ExecuteAsync<TResult>(
        string policyName,
        Func<ResilienceContext, ValueTask<TResult>> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var pipeline = _registry.GetPipeline(policyName);
        var startTime = Stopwatch.GetTimestamp();
        using var activity = ResilienceActivitySource.StartExecutionActivity(
            context.PolicyName,
            context.OperationName,
            context.TenantId,
            context.CorrelationId);

        try
        {
            LogStart(context);
            var result = await pipeline.ExecuteAsync(operation, context, cancellationToken).ConfigureAwait(false);
            RecordSuccess(context, startTime);
            return result;
        }
        catch (Exception ex)
        {
            RecordFailure(context, ex, startTime, activity);
            throw;
        }
    }

    /// <inheritdoc/>
    public ValueTask<TResult> ExecuteAsync<TResult>(
        string policyName,
        Func<CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var context = ResilienceContext.Create(policyName, cancellationToken);
        return ExecuteAsync(policyName, ctx => operation(ctx.CancellationToken), context, cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask ExecuteAsync(
        string policyName,
        Func<ResilienceContext, ValueTask> operation,
        ResilienceContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var pipeline = _registry.GetPipeline(policyName);
        var startTime = Stopwatch.GetTimestamp();
        using var activity = ResilienceActivitySource.StartExecutionActivity(
            context.PolicyName,
            context.OperationName,
            context.TenantId,
            context.CorrelationId);

        try
        {
            LogStart(context);
            await pipeline.ExecuteAsync(operation, context, cancellationToken).ConfigureAwait(false);
            RecordSuccess(context, startTime);
        }
        catch (Exception ex)
        {
            RecordFailure(context, ex, startTime, activity);
            throw;
        }
    }

    /// <inheritdoc/>
    public ValueTask ExecuteAsync(
        string policyName,
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var context = ResilienceContext.Create(policyName, cancellationToken);
        return ExecuteAsync(policyName, ctx => operation(ctx.CancellationToken), context, cancellationToken);
    }

    private void LogStart(ResilienceContext context)
    {
        if (_logger != null && _logger.IsEnabled(LogLevel.Trace))
        {
            _logExecutingTrace(
                _logger,
                context.OperationName,
                context.PolicyName,
                context.TenantId ?? "None",
                context.CorrelationId ?? "None",
                null);
        }
    }

    private void RecordSuccess(ResilienceContext context, long startTime)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

        ResilienceMeter.RecordExecution(
            context.PolicyName,
            context.OperationName,
            elapsedMs,
            isSuccess: true,
            context.TenantId);

        if (_logger != null && _logger.IsEnabled(LogLevel.Debug))
        {
            _logSuccessDebug(_logger, context.OperationName, context.PolicyName, elapsedMs, null);
        }
    }

    private void RecordFailure(ResilienceContext context, Exception ex, long startTime, Activity? activity)
    {
        var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;

        ResilienceMeter.RecordExecution(
            context.PolicyName,
            context.OperationName,
            elapsedMs,
            isSuccess: false,
            context.TenantId);

        RecordExceptionTelemetry(ex, context);
        LogException(ex, context, elapsedMs);

        ResilienceActivitySource.RecordException(activity, ex);
    }

    private static void RecordExceptionTelemetry(Exception ex, ResilienceContext context)
    {
        if (ex is ResilienceTimeoutException)
        {
            ResilienceMeter.RecordTimeout(context.PolicyName, context.OperationName, context.TenantId);
        }
        else if (ex is RateLimitRejectedException)
        {
            ResilienceMeter.RecordRateLimitRejection(context.PolicyName, context.OperationName, context.TenantId);
        }
        else if (ex is CircuitBrokenException)
        {
            ResilienceMeter.RecordCircuitBreakerRejection(context.PolicyName, context.OperationName, context.TenantId);
        }
    }

    private void LogException(Exception ex, ResilienceContext context, double elapsedMs)
    {
        if (_logger == null)
        {
            return;
        }

        if (ex is ResilienceTimeoutException timeoutEx)
        {
            _logTimeoutWarning(_logger, context.OperationName, context.PolicyName, timeoutEx.Timeout, elapsedMs, timeoutEx);
        }
        else if (ex is CircuitBrokenException cbEx)
        {
            _logCircuitBrokenWarning(_logger, context.OperationName, context.PolicyName, cbEx.RetryAfter?.ToString() ?? "Indefinite", cbEx);
        }
        else if (ex is RateLimitRejectedException rlEx)
        {
            _logRateLimitWarning(_logger, context.OperationName, context.PolicyName, rlEx.RetryAfter?.ToString() ?? "Unknown", rlEx);
        }
        else
        {
            _logFailedError(_logger, context.OperationName, context.PolicyName, elapsedMs, ex);
        }
    }
}
