// Copyright © Erickson Lopez. MIT License.
using System;
using global::Polly;

namespace EricksonLopez.Resilience.Polly.Adapters;

/// <summary>
/// Provides adapter methods to bridge <see cref="ResilienceContext"/> from the EricksonLopez ecosystem into Polly v8 <see cref="global::Polly.ResilienceContext"/>.
/// </summary>
public static class PollyContextAdapter
{
    private static readonly ResiliencePropertyKey<ResilienceContext> EcosystemContextKey =
        new("EricksonLopez.Resilience.EcosystemContext");

    /// <summary>
    /// Creates a Polly <see cref="global::Polly.ResilienceContext"/> wrapping the specified ecosystem context.
    /// </summary>
    /// <param name="ecosystemContext">The ecosystem resilience context.</param>
    /// <returns>A new Polly context instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ecosystemContext"/> is <see langword="null"/></exception>
    public static global::Polly.ResilienceContext ToPollyContext(ResilienceContext ecosystemContext)
    {
        ArgumentNullException.ThrowIfNull(ecosystemContext);

        var pollyContext = global::Polly.ResilienceContextPool.Shared.Get(
            operationKey: ecosystemContext.OperationName,
            cancellationToken: ecosystemContext.CancellationToken);

        pollyContext.Properties.Set(EcosystemContextKey, ecosystemContext);
        return pollyContext;
    }

    /// <summary>
    /// Extracts the ecosystem <see cref="ResilienceContext"/> from a Polly execution context, if present.
    /// </summary>
    /// <param name="pollyContext">The Polly context.</param>
    /// <returns>The original ecosystem context, or <see langword="null"/> if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pollyContext"/> is <see langword="null"/></exception>
    public static ResilienceContext? GetEcosystemContext(global::Polly.ResilienceContext pollyContext)
    {
        ArgumentNullException.ThrowIfNull(pollyContext);

        return pollyContext.Properties.TryGetValue(EcosystemContextKey, out var ctx)
            ? ctx
            : null;
    }

    /// <summary>
    /// Returns a pooled Polly context back to the shared pool.
    /// </summary>
    /// <param name="pollyContext">The Polly context to return.</param>
    public static void Return(global::Polly.ResilienceContext pollyContext)
    {
        if (pollyContext != null)
        {
            global::Polly.ResilienceContextPool.Shared.Return(pollyContext);
        }
    }
}
