// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for the strongly-typed fallback resilience strategy.
/// </summary>
/// <typeparam name="TResult">The type of value returned by the resilient operation and fallback generator.</typeparam>
public sealed class FallbackStrategyOptions<TResult> : ResilienceStrategyOptions
{
    /// <summary>
    /// Gets or sets the asynchronous delegate responsible for generating the fallback <typeparamref name="TResult"/> value.
    /// </summary>
    public Func<FallbackContext, ValueTask<TResult>>? FallbackAction { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate determining which exceptions trigger the fallback strategy.
    /// </summary>
    public Func<Exception, bool>? ShouldHandleException { get; set; }

    /// <summary>
    /// Gets or sets an optional predicate determining which outcome results trigger the fallback strategy.
    /// </summary>
    public Func<TResult, bool>? ShouldHandleResult { get; set; }

    /// <summary>
    /// Gets or sets an optional asynchronous callback invoked whenever the fallback action is triggered.
    /// </summary>
    public Func<FallbackContext, ValueTask>? OnFallback { get; set; }
}
