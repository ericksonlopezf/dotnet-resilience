// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.Resilience.Policies;

/// <summary>
/// Provides an abstract base class for defining typed resilience policies independent of underlying execution engines.
/// </summary>
[SuppressMessage("Major Code Smell", "S1694:An abstract class should have both abstract and concrete members", Justification = "Base class for typed policies for convenience and future non-abstract extension hooks")]
public abstract class ResiliencePolicy : IResiliencePolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePolicy"/> class.
    /// </summary>
    protected ResiliencePolicy()
    {
    }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract void Configure(IResiliencePipelineBuilder builder);
}
