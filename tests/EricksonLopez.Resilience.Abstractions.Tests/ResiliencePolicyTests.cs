// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Policies;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class ResiliencePolicyTests
{
    private sealed class TestPolicy : ResiliencePolicy
    {
        public override string Name => "custom-test-policy";

        public override void Configure(IResiliencePipelineBuilder builder)
        {
            builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(5) });
        }
    }

    [Fact]
    public void ConcreteResiliencePolicy_ImplementsContractCorrectly()
    {
        // Arrange
        var policy = new TestPolicy();
        var builder = Substitute.For<IResiliencePipelineBuilder>();

        // Act
        var name = policy.Name;
        policy.Configure(builder);

        // Assert
        name.Should().Be("custom-test-policy");
        policy.Should().BeAssignableTo<IResiliencePolicy>();
        builder.Received(1).AddTimeout(Arg.Is<TimeoutStrategyOptions>(o => o.Timeout == TimeSpan.FromSeconds(5)));
    }
}
