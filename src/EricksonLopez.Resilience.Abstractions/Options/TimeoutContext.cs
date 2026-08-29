// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Provides context information when an operation execution exceeds the configured timeout duration.
/// </summary>
public sealed class TimeoutContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutContext"/> class.
    /// </summary>
    /// <param name="resilienceContext">The active execution context.</param>
    /// <param name="timeout">The configured timeout duration that was exceeded.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resilienceContext"/> is <see langword="null"/></exception>
    public TimeoutContext(ResilienceContext resilienceContext, TimeSpan timeout)
    {
        ResilienceContext = resilienceContext ?? throw new ArgumentNullException(nameof(resilienceContext));
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the resilience execution context.
    /// </summary>
    public ResilienceContext ResilienceContext { get; }

    /// <summary>
    /// Gets the timeout duration that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; }
}
