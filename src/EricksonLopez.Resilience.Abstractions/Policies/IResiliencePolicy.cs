// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Policies;

/// <summary>
/// Defines a strongly-typed, reusable resilience policy definition within the EricksonLopez ecosystem.
/// </summary>
public interface IResiliencePolicy
{
    /// <summary>
    /// Gets the unique logical name of the resilience policy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Configures the resilience strategies within the provided pipeline builder.
    /// </summary>
    /// <param name="builder">The resilience pipeline builder.</param>
    void Configure(IResiliencePipelineBuilder builder);
}
