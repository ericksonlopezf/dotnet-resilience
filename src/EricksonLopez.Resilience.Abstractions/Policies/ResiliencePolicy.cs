// Copyright © Erickson Lopez. MIT License.

namespace EricksonLopez.Resilience.Policies;

/// <summary>
/// Provides an abstract base class for defining typed resilience policies independent of underlying execution engines.
/// </summary>
public abstract class ResiliencePolicy : IResiliencePolicy
{
    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract void Configure(IResiliencePipelineBuilder builder);
}
