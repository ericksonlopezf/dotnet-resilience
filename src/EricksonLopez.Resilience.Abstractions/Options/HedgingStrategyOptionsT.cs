// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for strongly-typed parallel speculative hedging.
/// </summary>
/// <typeparam name="TResult">The type of value returned by the hedged operation.</typeparam>
/// <remarks>
/// Parallel hedging executes concurrent speculative attempts if the primary execution does not complete within <see cref="Delay"/>.
/// WARNING: Hedging must only be configured for safe, idempotent, and side-effect-free operations (such as read-only queries).
/// </remarks>
public sealed class HedgingStrategyOptions<TResult> : ResilienceStrategyOptions
{
    /// <summary>
    /// Gets or sets the maximum number of additional concurrent hedged attempts allowed.
    /// Default is 2 attempts.
    /// </summary>
    public int MaxHedgedAttempts { get; set; } = 2;

    /// <summary>
    /// Gets or sets the delay duration before spawning the next hedged execution attempt.
    /// Default is 500 milliseconds.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an exception triggers a fast hedge without waiting for delay.
    /// </summary>
    public Func<Exception, bool>? ShouldHandleException { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an outcome result triggers a fast hedge.
    /// </summary>
    public Func<TResult, bool>? ShouldHandleResult { get; set; }

    /// <summary>
    /// Gets or sets an optional asynchronous callback invoked when a hedged attempt is initiated.
    /// </summary>
    public Func<HedgingContext, ValueTask>? OnHedging { get; set; }

    /// <summary>
    /// Gets or sets an optional custom delegate generator providing secondary speculative tasks (e.g. hitting alternate replicas).
    /// </summary>
    public Func<HedgingContext, Func<ValueTask<TResult>>?>? HedgedActionGenerator { get; set; }
}
