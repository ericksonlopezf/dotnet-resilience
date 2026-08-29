// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Options;

/// <summary>
/// Provides contextual information when a hedged execution attempt is initiated.
/// </summary>
public sealed class HedgingContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HedgingContext"/> class.
    /// </summary>
    /// <param name="resilienceContext">The active execution context.</param>
    /// <param name="attemptNumber">The 1-based attempt number of the hedged execution.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resilienceContext"/> is <see langword="null"/></exception>
    public HedgingContext(ResilienceContext resilienceContext, int attemptNumber)
    {
        ResilienceContext = resilienceContext ?? throw new ArgumentNullException(nameof(resilienceContext));
        AttemptNumber = attemptNumber;
    }

    /// <summary>
    /// Gets the execution context.
    /// </summary>
    public ResilienceContext ResilienceContext { get; }

    /// <summary>
    /// Gets the 1-based index of the hedged attempt being spawned.
    /// </summary>
    public int AttemptNumber { get; }
}
