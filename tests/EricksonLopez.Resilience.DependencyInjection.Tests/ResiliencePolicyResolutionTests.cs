// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.DependencyInjection.Tests;

public sealed class ResiliencePolicyResolutionTests
{
    [Fact]
    public void Resolve_ResiliencePipelineRegistry_ResolvesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        var provider = services.BuildServiceProvider();

        // Act
        var reg1 = provider.GetRequiredService<IResiliencePipelineRegistry>();
        var reg2 = provider.GetRequiredService<IResiliencePipelineRegistry>();

        // Assert
        reg1.Should().BeSameAs(reg2);
    }
}
