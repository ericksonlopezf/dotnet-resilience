// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using EricksonLopez.Resilience.Exceptions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class ResiliencePipelineRegistryDefaultInterfaceTests
{
    private sealed class MinimalPipelineRegistry : IResiliencePipelineRegistry
    {
        public IResiliencePipeline GetPipeline(string policyName) => throw new NotImplementedException();
        public bool TryGetPipeline(string policyName, [NotNullWhen(true)] out IResiliencePipeline? pipeline)
        {
            pipeline = null;
            return false;
        }
    }

    private sealed class OverriddenTypedPipelineRegistry : IResiliencePipelineRegistry
    {
        private readonly IResiliencePipeline<string> _typedPipeline;

        public OverriddenTypedPipelineRegistry(IResiliencePipeline<string> typedPipeline)
        {
            _typedPipeline = typedPipeline;
        }

        public IResiliencePipeline GetPipeline(string policyName) => throw new NotImplementedException();
        public bool TryGetPipeline(string policyName, [NotNullWhen(true)] out IResiliencePipeline? pipeline)
        {
            pipeline = null;
            return false;
        }

        public bool TryGetPipeline<TResult>(string policyName, [NotNullWhen(true)] out IResiliencePipeline<TResult>? pipeline)
        {
            if (policyName == "valid-policy" && typeof(TResult) == typeof(string))
            {
                pipeline = (IResiliencePipeline<TResult>)_typedPipeline;
                return true;
            }

            pipeline = null;
            return false;
        }
    }

    [Fact]
    public void DefaultInterfaceMethod_TryGetPipelineTResult_ReturnsFalseAndNull()
    {
        // Arrange
        IResiliencePipelineRegistry registry = new MinimalPipelineRegistry();

        // Act
        var result = registry.TryGetPipeline<string>("test-policy", out var pipeline);

        // Assert
        result.Should().BeFalse();
        pipeline.Should().BeNull();
    }

    [Fact]
    public void DefaultInterfaceMethod_GetPipelineTResult_WhenNotFound_ThrowsResiliencePolicyNotFoundException()
    {
        // Arrange
        IResiliencePipelineRegistry registry = new MinimalPipelineRegistry();

        // Act & Assert
        var act = () => registry.GetPipeline<string>("missing-policy");
        act.Should().Throw<ResiliencePolicyNotFoundException>()
            .Which.PolicyName.Should().Be("missing-policy");
    }

    [Fact]
    public void DefaultInterfaceMethod_GetPipelineTResult_WhenFound_ReturnsPipeline()
    {
        // Arrange
        var mockTypedPipeline = Substitute.For<IResiliencePipeline<string>>();
        IResiliencePipelineRegistry registry = new OverriddenTypedPipelineRegistry(mockTypedPipeline);

        // Act
        var pipeline = registry.GetPipeline<string>("valid-policy");

        // Assert
        pipeline.Should().BeSameAs(mockTypedPipeline);
    }
}
