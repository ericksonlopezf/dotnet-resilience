// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EricksonLopez.Resilience.Policies;

namespace EricksonLopez.Resilience.Registry;

/// <summary>
/// Represents a registry holding declarative <see cref="IResiliencePolicy"/> definitions before they are compiled into runtime pipelines.
/// </summary>
public sealed class ResiliencePolicyRegistry
{
    private readonly ConcurrentDictionary<string, IResiliencePolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all registered policy definitions.
    /// </summary>
    public IReadOnlyCollection<IResiliencePolicy> Policies => _policies.Values.ToArray();

    /// <summary>
    /// Registers a policy definition in the registry.
    /// </summary>
    /// <param name="policy">The policy instance to register.</param>
    /// <returns>The policy registry instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException">The name of <paramref name="policy"/> is <see langword="null"/> or whitespace</exception>
    public ResiliencePolicyRegistry Register(IResiliencePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentException.ThrowIfNullOrWhiteSpace(policy.Name);

        _policies[policy.Name] = policy;
        return this;
    }

    /// <summary>
    /// Attempts to retrieve a registered policy definition by its name.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <param name="policy">When this method returns, contains the policy if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the policy exists; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public bool TryGetPolicy(string policyName, [NotNullWhen(true)] out IResiliencePolicy? policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        return _policies.TryGetValue(policyName, out policy);
    }
}
