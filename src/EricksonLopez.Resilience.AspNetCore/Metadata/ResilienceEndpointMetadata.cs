// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.AspNetCore.Metadata;

/// <summary>
/// Represents endpoint metadata identifying the resilience policy associated with an ASP.NET Core HTTP endpoint.
/// </summary>
public sealed class ResilienceEndpointMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceEndpointMetadata"/> class.
    /// </summary>
    /// <param name="policyName">The name of the resilience policy.</param>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public ResilienceEndpointMetadata(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        PolicyName = policyName;
    }

    /// <summary>
    /// Gets the name of the resilience policy.
    /// </summary>
    public string PolicyName { get; }
}
