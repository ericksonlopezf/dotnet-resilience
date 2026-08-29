// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Resilience.Policies;

namespace EricksonLopez.Resilience.DependencyInjection;

/// <summary>
/// Represents options for configuring the EricksonLopez resilience system during dependency injection setup.
/// </summary>
public sealed class ResilienceOptions
{
    internal List<NamedPolicyRegistration> NamedRegistrations { get; } = new();

    /// <summary>
    /// Registers a named resilience policy configured via a builder delegate.
    /// </summary>
    /// <param name="policyName">The unique policy name.</param>
    /// <param name="configure">The builder configuration action.</param>
    /// <returns>The options instance for method chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/></exception>
    public ResilienceOptions AddPolicy(string policyName, Action<IResiliencePipelineBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(configure);

        NamedRegistrations.Add(new NamedPolicyRegistration(policyName, configure));

        return this;
    }
}
