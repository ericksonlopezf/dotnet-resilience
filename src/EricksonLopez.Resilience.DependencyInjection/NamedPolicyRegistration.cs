// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Resilience.DependencyInjection;

/// <summary>
/// Represents an internal registration for a named resilience policy configured via a delegate.
/// </summary>
internal sealed class NamedPolicyRegistration
{
    public NamedPolicyRegistration(string name, Action<IResiliencePipelineBuilder> configure)
    {
        Name = name;
        Configure = (builder, _) => configure(builder);
    }

    public NamedPolicyRegistration(string name, Action<IResiliencePipelineBuilder, IServiceProvider> configure)
    {
        Name = name;
        Configure = configure;
    }

    public string Name { get; }
    public Action<IResiliencePipelineBuilder, IServiceProvider> Configure { get; }
}
