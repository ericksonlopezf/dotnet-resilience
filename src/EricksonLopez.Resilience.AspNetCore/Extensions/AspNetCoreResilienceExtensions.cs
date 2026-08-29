// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Resilience.AspNetCore.Metadata;
using Microsoft.AspNetCore.Builder;

namespace EricksonLopez.Resilience.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for attaching resilience metadata to ASP.NET Core endpoint builders.
/// </summary>
public static class AspNetCoreResilienceExtensions
{
    /// <summary>
    /// Attaches resilience policy metadata to the endpoint convention builder.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the endpoint convention builder.</typeparam>
    /// <param name="builder">The endpoint builder.</param>
    /// <param name="policyName">The name of the resilience policy.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public static TBuilder RequireResilience<TBuilder>(
        this TBuilder builder,
        string policyName)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new ResilienceEndpointMetadata(policyName));
        return builder;
    }
}
