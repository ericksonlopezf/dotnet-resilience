// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.AspNetCore.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EricksonLopez.Resilience.AspNetCore.Tests;

public sealed class AspNetCoreResilienceExtensionsTests
{
    private sealed class MockEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public List<object> Metadata { get; } = new();

        public void Add(Action<EndpointBuilder> convention)
        {
            var builder = new TestEndpointBuilder();
            convention(builder);
            Metadata.AddRange(builder.Metadata);
        }

        private sealed class TestEndpointBuilder : EndpointBuilder
        {
            public override Endpoint Build() => throw new NotImplementedException();
        }
    }

    #region ResilienceEndpointMetadata

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResilienceEndpointMetadata_NullOrWhitespace_ThrowsArgumentException(string? policyName)
    {
        Action act = () => _ = new ResilienceEndpointMetadata(policyName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ResilienceEndpointMetadata_ValidPolicyName_SetsProperty()
    {
        var metadata = new ResilienceEndpointMetadata("order-policy");
        metadata.PolicyName.Should().Be("order-policy");
    }

    #endregion

    #region AspNetCoreResilienceExtensions

    [Fact]
    public void RequireResilience_NullBuilder_ThrowsArgumentNullException()
    {
        IEndpointConventionBuilder? builder = null;
        Action act = () => builder!.RequireResilience("test-policy");
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequireResilience_NullOrWhitespacePolicyName_ThrowsArgumentException(string? policyName)
    {
        var builder = new MockEndpointConventionBuilder();
        Action act = () => builder.RequireResilience(policyName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RequireResilience_AttachesResilienceEndpointMetadata_AndReturnsSameBuilder()
    {
        // Arrange
        var builder = new MockEndpointConventionBuilder();

        // Act
        var returned = builder.RequireResilience("weather-policy");

        // Assert
        returned.Should().BeSameAs(builder);
        builder.Metadata.Should().ContainSingle()
            .Which.Should().BeOfType<ResilienceEndpointMetadata>()
            .Which.PolicyName.Should().Be("weather-policy");
    }

    #endregion
}
