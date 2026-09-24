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

    [Fact]
    public void Constructor_WithServiceProviderAction_InitializesPropertiesCorrectly()
    {
        // Arrange
        var executed = false;
        IServiceProvider? capturedSp = null;
        Action<IResiliencePipelineBuilder, IServiceProvider> configure = (b, sp) =>
        {
            executed = true;
            capturedSp = sp;
        };

        // Act
        var reg = new NamedPolicyRegistration("test-sp-policy", configure);

        // Assert
        reg.Name.Should().Be("test-sp-policy");
        reg.Configure.Should().NotBeNull();

        var builder = new ResiliencePipelineBuilder("test-sp-policy");
        var spMock = NSubstitute.Substitute.For<IServiceProvider>();
        reg.Configure(builder, spMock);

        executed.Should().BeTrue();
        capturedSp.Should().BeSameAs(spMock);
    }
}
