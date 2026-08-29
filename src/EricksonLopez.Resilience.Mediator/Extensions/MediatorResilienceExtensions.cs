// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Mediator.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Mediator.Extensions;

/// <summary>
/// Provides extension methods for registering resilience pipeline behaviors into the mediator pipeline.
/// </summary>
public static class MediatorResilienceExtensions
{
    /// <summary>
    /// Registers the <see cref="ResiliencePipelineBehavior{TRequest, TResponse}"/> as an open generic pipeline behavior in DI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddResiliencePipelineBehavior(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ResiliencePipelineBehavior<,>));
        return services;
    }
}
