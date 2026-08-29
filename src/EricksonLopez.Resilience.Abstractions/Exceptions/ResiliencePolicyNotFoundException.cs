// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.Exceptions;

/// <summary>
/// Represents the exception thrown when a requested resilience policy name cannot be resolved from the registry.
/// </summary>
public sealed class ResiliencePolicyNotFoundException : ResilienceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePolicyNotFoundException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the unresolved policy.</param>
    public ResiliencePolicyNotFoundException(string policyName)
        : base($"Resilience policy '{policyName}' was not found in the registry. Ensure it is configured and registered during application startup.")
    {
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the name of the unresolved policy.
    /// </summary>
    public string PolicyName { get; }
}
