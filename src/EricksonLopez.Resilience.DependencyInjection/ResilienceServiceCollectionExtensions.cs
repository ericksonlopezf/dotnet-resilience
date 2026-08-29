// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.Policies;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Registration;
using EricksonLopez.Resilience.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EricksonLopez.Resilience.DependencyInjection;

/// <summary>
/// Provides extension methods for registering EricksonLopez resilience services into <see cref="IServiceCollection"/>.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core resilience infrastructure, Polly execution adapter, pipeline registries, and error classifiers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional configuration delegate for registering initial policies.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddEricksonLopezResilience(
        this IServiceCollection services,
        Action<ResilienceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Ensure Polly is registered as the default execution pipeline compiler
        PollyResilienceRegistration.Initialize();

        services.TryAddSingleton<ResiliencePolicyRegistry>();

        services.TryAddSingleton<ResiliencePipelineRegistry>(sp =>
        {
            var registry = new ResiliencePipelineRegistry();
            var policyRegistry = sp.GetRequiredService<ResiliencePolicyRegistry>();

            // Compile all registered typed policies
            foreach (var policy in sp.GetServices<IResiliencePolicy>())
            {
                policyRegistry.Register(policy);
                var builder = new ResiliencePipelineBuilder(policy.Name);
                policy.Configure(builder);
                var pipeline = builder.Build();
                registry.Register(policy.Name, pipeline);
            }

            // Compile all named delegate policies
            foreach (var named in sp.GetServices<NamedPolicyRegistration>())
            {
                var builder = new ResiliencePipelineBuilder(named.Name);
                named.Configure(builder);
                var pipeline = builder.Build();
                registry.Register(named.Name, pipeline);
            }

            return registry;
        });

        services.TryAddSingleton<IResiliencePipelineRegistry>(sp => sp.GetRequiredService<ResiliencePipelineRegistry>());

        services.TryAddSingleton<ResultRetryClassifier>();
        services.TryAddSingleton<IResultRetryClassifier>(sp => sp.GetRequiredService<ResultRetryClassifier>());
        services.TryAddSingleton<IErrorClassifier>(sp => sp.GetRequiredService<ResultRetryClassifier>());

        services.TryAddSingleton<IResilienceExecutor>(sp =>
            new PollyResilienceExecutor(
                sp.GetRequiredService<IResiliencePipelineRegistry>(),
                sp.GetService<Microsoft.Extensions.Logging.ILogger<PollyResilienceExecutor>>()));

        if (configure != null)
        {
            var options = new ResilienceOptions();
            configure(options);
            foreach (var reg in options.NamedRegistrations)
            {
                services.AddSingleton(reg);
            }
        }

        return services;
    }

    /// <summary>
    /// Registers a strongly-typed resilience policy and compiles it into the runtime pipeline registry.
    /// </summary>
    /// <typeparam name="TPolicy">The type of the resilience policy.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddResiliencePolicy<TPolicy>(this IServiceCollection services)
        where TPolicy : class, IResiliencePolicy, new()
    {
        services.AddEricksonLopezResilience();
        services.AddSingleton<IResiliencePolicy>(new TPolicy());

        return services;
    }

    /// <summary>
    /// Registers a named resilience policy configured via an action delegate and compiles it into the runtime pipeline registry.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="policyName">The unique policy name.</param>
    /// <param name="configure">The builder configuration action.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public static IServiceCollection AddResiliencePolicy(
        this IServiceCollection services,
        string policyName,
        Action<IResiliencePipelineBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddEricksonLopezResilience();
        services.AddSingleton(new NamedPolicyRegistration(policyName, configure));

        return services;
    }
}
