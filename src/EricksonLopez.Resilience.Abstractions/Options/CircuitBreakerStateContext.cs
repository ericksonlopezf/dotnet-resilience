// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Provides contextual information when a circuit breaker state transition occurs.
/// </summary>
public sealed class CircuitBreakerStateContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerStateContext"/> class.
    /// </summary>
    /// <param name="resilienceContext">The active execution context, if available during transition.</param>
    /// <param name="state">The new state the circuit breaker transitioned into.</param>
    /// <param name="breakDuration">The remaining or configured duration of the break if the circuit opened.</param>
    /// <param name="lastException">The last exception observed prior to state transition, if any.</param>
    public CircuitBreakerStateContext(
        ResilienceContext? resilienceContext,
        CircuitBreakerState state,
        TimeSpan? breakDuration = null,
        Exception? lastException = null)
    {
        ResilienceContext = resilienceContext;
        State = state;
        BreakDuration = breakDuration;
        LastException = lastException;
    }

    /// <summary>
    /// Gets the execution context associated with the transition, if available.
    /// </summary>
    public ResilienceContext? ResilienceContext { get; }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State { get; }

    /// <summary>
    /// Gets the duration the circuit will remain open before attempting a half-open trial.
    /// </summary>
    public TimeSpan? BreakDuration { get; }

    /// <summary>
    /// Gets the last exception associated with the state change.
    /// </summary>
    public Exception? LastException { get; }
}
