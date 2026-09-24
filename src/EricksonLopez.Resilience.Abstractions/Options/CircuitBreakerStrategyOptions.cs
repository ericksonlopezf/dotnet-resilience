// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for the circuit breaker resilience strategy.
/// </summary>
public sealed class CircuitBreakerStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerStrategyOptions"/> class with default settings.
    /// </summary>
    public CircuitBreakerStrategyOptions()
    {
    }

    /// <summary>
    /// Gets or sets the failure ratio (between 0.0 and 1.0) that causes the circuit to open.
    /// Default is 0.5 (50% failure rate).
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the minimum number of execution attempts within the sampling duration required before the failure ratio is evaluated.
    /// Default is 20 throughput executions.
    /// </summary>
    public int MinimumThroughput { get; set; } = 20;

    /// <summary>
    /// Gets or sets the duration of the rolling sampling window over which failure statistics are measured.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the duration the circuit remains open before transitioning to half-open state.
    /// Default is 10 seconds.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an observed exception contributes to the failure ratio.
    /// </summary>
    public Func<Exception, bool>? ShouldHandleException { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an observed result value contributes to the failure ratio.
    /// </summary>
    public Func<object?, bool>? ShouldHandleResult { get; set; }

    /// <summary>
    /// Gets or sets an asynchronous callback invoked when the circuit transitions to the <see cref="CircuitBreakerState.Open"/> state.
    /// </summary>
    public Func<CircuitBreakerStateContext, ValueTask>? OnCircuitOpened { get; set; }

    /// <summary>
    /// Gets or sets an asynchronous callback invoked when the circuit transitions to the <see cref="CircuitBreakerState.Closed"/> state.
    /// </summary>
    public Func<CircuitBreakerStateContext, ValueTask>? OnCircuitClosed { get; set; }

    /// <summary>
    /// Gets or sets an asynchronous callback invoked when the circuit transitions to the <see cref="CircuitBreakerState.HalfOpen"/> state.
    /// </summary>
    public Func<CircuitBreakerStateContext, ValueTask>? OnCircuitHalfOpened { get; set; }
}
