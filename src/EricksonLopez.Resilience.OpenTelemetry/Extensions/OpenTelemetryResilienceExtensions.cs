// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Options;

namespace EricksonLopez.Resilience.OpenTelemetry.Extensions;

/// <summary>
/// Provides extension methods for attaching OpenTelemetry metrics and tracing callbacks to resilience strategy options.
/// </summary>
public static class OpenTelemetryResilienceExtensions
{
    /// <summary>
    /// Attaches OpenTelemetry telemetry recording hooks to the configured retry strategy options.
    /// </summary>
    /// <param name="options">The retry strategy options.</param>
    /// <returns>The options instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static RetryStrategyOptions WithTelemetry(this RetryStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var originalOnRetry = options.OnRetry;
        options.OnRetry = async context =>
        {
            ResilienceMeter.RecordRetry(
                context.ResilienceContext.PolicyName,
                context.ResilienceContext.OperationName,
                context.AttemptNumber,
                context.ResilienceContext.TenantId);

            if (originalOnRetry != null)
            {
                await originalOnRetry(context).ConfigureAwait(false);
            }
        };

        return options;
    }

    /// <summary>
    /// Attaches OpenTelemetry telemetry recording hooks to the configured circuit breaker strategy options.
    /// </summary>
    /// <param name="options">The circuit breaker strategy options.</param>
    /// <returns>The options instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static CircuitBreakerStrategyOptions WithTelemetry(this CircuitBreakerStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var originalOnOpened = options.OnCircuitOpened;
        options.OnCircuitOpened = async context =>
        {
            ResilienceMeter.RecordCircuitStateChange(
                context.ResilienceContext?.PolicyName ?? options.Name ?? "CircuitBreaker",
                CircuitBreakerState.Open,
                context.ResilienceContext?.TenantId);

            if (originalOnOpened != null)
            {
                await originalOnOpened(context).ConfigureAwait(false);
            }
        };

        var originalOnClosed = options.OnCircuitClosed;
        options.OnCircuitClosed = async context =>
        {
            ResilienceMeter.RecordCircuitStateChange(
                context.ResilienceContext?.PolicyName ?? options.Name ?? "CircuitBreaker",
                CircuitBreakerState.Closed,
                context.ResilienceContext?.TenantId);

            if (originalOnClosed != null)
            {
                await originalOnClosed(context).ConfigureAwait(false);
            }
        };

        var originalOnHalfOpened = options.OnCircuitHalfOpened;
        options.OnCircuitHalfOpened = async context =>
        {
            ResilienceMeter.RecordCircuitStateChange(
                context.ResilienceContext?.PolicyName ?? options.Name ?? "CircuitBreaker",
                CircuitBreakerState.HalfOpen,
                context.ResilienceContext?.TenantId);

            if (originalOnHalfOpened != null)
            {
                await originalOnHalfOpened(context).ConfigureAwait(false);
            }
        };

        return options;
    }

    /// <summary>
    /// Attaches OpenTelemetry telemetry recording hooks to the configured timeout strategy options.
    /// </summary>
    /// <param name="options">The timeout strategy options.</param>
    /// <returns>The options instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static TimeoutStrategyOptions WithTelemetry(this TimeoutStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var originalOnTimeout = options.OnTimeout;
        options.OnTimeout = async context =>
        {
            ResilienceMeter.RecordTimeout(
                context.ResilienceContext.PolicyName,
                context.ResilienceContext.OperationName,
                context.ResilienceContext.TenantId);

            if (originalOnTimeout != null)
            {
                await originalOnTimeout(context).ConfigureAwait(false);
            }
        };

        return options;
    }
}
