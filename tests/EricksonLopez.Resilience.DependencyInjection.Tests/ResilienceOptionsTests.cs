// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.DependencyInjection.Tests;

public sealed class ResilienceOptionsTests
{
    [Fact]
    public void AddPolicy_WithValidArguments_AddsNamedRegistrationAndReturnsSelf()
    {
        // Arrange
        var options = new ResilienceOptions();
        var executed = false;
        Action<IResiliencePipelineBuilder> configure = b => executed = true;

        // Act
        var result = options.AddPolicy("test-policy", configure);

        // Assert
        result.Should().BeSameAs(options);
        options.NamedRegistrations.Should().HaveCount(1);
        options.NamedRegistrations[0].Name.Should().Be("test-policy");
        options.NamedRegistrations[0].Configure.Should().BeSameAs(configure);

        var builder = new ResiliencePipelineBuilder("test-policy");
        options.NamedRegistrations[0].Configure(builder);
        executed.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddPolicy_WithNullOrWhitespaceName_ThrowsArgumentException(string? policyName)
    {
        // Arrange
        var options = new ResilienceOptions();

        // Act
        Action act = () => options.AddPolicy(policyName!, b => { });

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddPolicy_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ResilienceOptions();

        // Act
        Action act = () => options.AddPolicy("valid-name", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
