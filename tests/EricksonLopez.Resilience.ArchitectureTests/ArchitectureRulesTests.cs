// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Adapters;
using NetArchTest.Rules;
using Xunit;

namespace EricksonLopez.Resilience.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    private static readonly Assembly AbstractionsAssembly = typeof(IResilienceExecutor).Assembly;
    private static readonly Assembly CoreAssembly = typeof(ResiliencePipelineBuilder).Assembly;
    private static readonly Assembly PollyAdapterAssembly = typeof(PollyResiliencePipeline).Assembly;

    [Fact]
    public void Abstractions_MustNotReference_Polly()
    {
        var referenced = AbstractionsAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        referenced.Should().NotContain(name => name.Contains("Polly", StringComparison.OrdinalIgnoreCase),
            "Resilience.Abstractions must be completely decoupled from Polly.");
    }

    [Fact]
    public void Abstractions_MustNotReference_AspNetCore()
    {
        var referenced = AbstractionsAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        referenced.Should().NotContain(name => name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase),
            "Resilience.Abstractions must not depend on ASP.NET Core.");
    }

    [Fact]
    public void Core_MustNotReference_Polly()
    {
        var referenced = CoreAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        referenced.Should().NotContain(name => name.Contains("Polly", StringComparison.OrdinalIgnoreCase),
            "Resilience Core must be completely decoupled from Polly.");
    }

    [Fact]
    public void Core_MustNotReference_AspNetCore()
    {
        var referenced = CoreAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        referenced.Should().NotContain(name => name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase),
            "Resilience Core must not depend on ASP.NET Core.");
    }

    [Fact]
    public void PollyAdapter_IsTheOnlyResilienceAssembly_ReferencingPolly()
    {
        var referenced = PollyAdapterAssembly.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        referenced.Should().Contain(name => name.Contains("Polly", StringComparison.OrdinalIgnoreCase),
            "Resilience.Polly is the dedicated infrastructure adapter referencing Polly.");
    }

    [Fact]
    public void AllPublicTypes_InAbstractions_AreProperlyStructured()
    {
        var result = Types.InAssembly(AbstractionsAssembly)
            .That()
            .ArePublic()
            .ShouldNot()
            .HaveDependencyOn("Polly")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("No public type in Abstractions may have Polly dependencies.");
    }
}
