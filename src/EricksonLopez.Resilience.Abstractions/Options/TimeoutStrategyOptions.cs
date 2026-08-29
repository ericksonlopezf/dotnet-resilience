// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Defines configuration options for the timeout resilience strategy.
/// </summary>
public sealed class TimeoutStrategyOptions : ResilienceStrategyOptions
{
    /// <summary>
    /// Gets or sets the maximum execution duration permitted before cancelling the operation and throwing a <see cref="Exceptions.ResilienceTimeoutException"/>.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets an asynchronous callback invoked when an operation execution times out.
    /// </summary>
    public Func<TimeoutContext, ValueTask>? OnTimeout { get; set; }
}
