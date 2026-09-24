// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.DependencyInjection.Tests;

public sealed class NamedPolicyRegistrationTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange
        var executed = false;
        Action<IResiliencePipelineBuilder> configure = b => executed = true;

        // Act
        var reg = new NamedPolicyRegistration("test-policy", configure);

        // Assert
        reg.Name.Should().Be("test-policy");
        reg.Configure.Should().NotBeNull();

        var builder = new ResiliencePipelineBuilder("test-policy");
        reg.Configure(builder, null!);
        executed.Should().BeTrue();
    }
}
