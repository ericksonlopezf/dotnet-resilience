// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for the untyped hedging resilience strategy.
/// </summary>
/// <remarks>
/// In untyped pipelines, hedging operates as a speculative retry mechanism — the pipeline
/// re-executes the same delegate sequentially after <see cref="Delay"/> if the previous attempt fails.
/// This does <b>not</b> launch parallel concurrent executions; that behavior requires a typed pipeline
/// via <see cref="HedgingStrategyOptions{TResult}"/>.
/// WARNING: Hedging must only be configured for safe, idempotent, and side-effect-free operations (such as read-only queries).
/// </remarks>
public sealed class HedgingStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HedgingStrategyOptions"/> class with default settings.
    /// </summary>
    public HedgingStrategyOptions()
    {
    }

    /// <summary>
    /// Gets or sets the maximum number of additional speculative retry attempts allowed.
    /// Default is 2 attempts.
    /// </summary>
    public int MaxHedgedAttempts { get; set; } = 2;

    /// <summary>
    /// Gets or sets the delay duration before spawning the next speculative attempt.
    /// Default is 500 milliseconds.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an exception triggers a fast retry without waiting for delay.
    /// </summary>
    public Func<Exception, bool>? ShouldHandleException { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate to determine whether an outcome result triggers a fast retry.
    /// </summary>
    public Func<object?, bool>? ShouldHandleResult { get; set; }
}
