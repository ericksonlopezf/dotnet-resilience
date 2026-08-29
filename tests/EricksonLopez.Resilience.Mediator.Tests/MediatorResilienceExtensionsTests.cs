// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.Mediator.Tests;

public sealed class MediatorResilienceExtensionsTests
{
    [Fact]
    public void AddResiliencePipelineBehavior_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        Action act = () => services!.AddResiliencePipelineBehavior();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddResiliencePipelineBehavior_RegistersPipelineBehaviorInDI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var returned = services.AddResiliencePipelineBehavior();

        // Assert
        returned.Should().BeSameAs(services);
        var descriptor = services.Should().Contain(d => d.ServiceType == typeof(IPipelineBehavior<,>)).Which;
        descriptor.ImplementationType.Should().Be(typeof(ResiliencePipelineBehavior<,>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }
}
