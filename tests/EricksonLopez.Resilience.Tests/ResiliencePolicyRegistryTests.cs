// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Policies;
using EricksonLopez.Resilience.Registry;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class ResiliencePolicyRegistryTests
{
    [Fact]
    public void Register_And_TryGetPolicy_ReturnsRegisteredPolicy()
    {
        // Arrange
        var registry = new ResiliencePolicyRegistry();
        var mockPolicy = Substitute.For<IResiliencePolicy>();
        mockPolicy.Name.Returns("order-policy");

        // Act
        var returned = registry.Register(mockPolicy);
        var found = registry.TryGetPolicy("order-policy", out var resolved);
        var foundCaseInsensitive = registry.TryGetPolicy("ORDER-POLICY", out var resolvedCase);

        // Assert
        returned.Should().BeSameAs(registry);
        registry.Policies.Should().ContainSingle().Which.Should().BeSameAs(mockPolicy);
        found.Should().BeTrue();
        resolved.Should().BeSameAs(mockPolicy);
        foundCaseInsensitive.Should().BeTrue();
        resolvedCase.Should().BeSameAs(mockPolicy);
    }

    [Fact]
    public void Register_WithInvalidPolicy_ThrowsExceptions()
    {
        // Arrange
        var registry = new ResiliencePolicyRegistry();
        var nullNamePolicy = Substitute.For<IResiliencePolicy>();
        nullNamePolicy.Name.Returns((string)null!);

        var whitespaceNamePolicy = Substitute.For<IResiliencePolicy>();
        whitespaceNamePolicy.Name.Returns("   ");

        // Act & Assert
        var actNull = () => registry.Register(null!);
        var actNullName = () => registry.Register(nullNamePolicy);
        var actWhitespaceName = () => registry.Register(whitespaceNamePolicy);

        actNull.Should().Throw<ArgumentNullException>();
        actNullName.Should().Throw<ArgumentNullException>();
        actWhitespaceName.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetPolicy_WhenMissing_ReturnsFalseAndNull()
    {
        // Arrange
        var registry = new ResiliencePolicyRegistry();

        // Act
        var found = registry.TryGetPolicy("missing-policy", out var resolved);

        // Assert
        found.Should().BeFalse();
        resolved.Should().BeNull();
        registry.Policies.Should().BeEmpty();
    }

    [Fact]
    public void TryGetPolicy_WithInvalidPolicyName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new ResiliencePolicyRegistry();

        // Act & Assert
        var act1 = () => registry.TryGetPolicy(null!, out _);
        var act2 = () => registry.TryGetPolicy(string.Empty, out _);
        var act3 = () => registry.TryGetPolicy("   ", out _);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }
}
