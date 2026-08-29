// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Specifies the operational state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState : byte
{
    /// <summary>
    /// Indicates that the circuit is closed and executions are permitted normally.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Indicates that the circuit is open and executions are short-circuited and rejected immediately.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Indicates that the circuit is half-open and a limited number of trial executions are permitted to evaluate system recovery.
    /// </summary>
    HalfOpen = 2,

    /// <summary>
    /// Indicates that the circuit is manually isolated and all executions are rejected until manually closed.
    /// </summary>
    Isolated = 3
}
