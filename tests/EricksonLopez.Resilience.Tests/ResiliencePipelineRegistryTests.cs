// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Pipelines;
using EricksonLopez.Resilience.Registry;
using Xunit;

namespace EricksonLopez.Resilience.Tests;

public sealed class ResiliencePipelineRegistryTests
{
    [Fact]
    public void Register_And_GetPipeline_ReturnsRegisteredInstance()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline("payment-api");

        // Act
        var returned = registry.Register("payment-api", pipeline);
        var resolved = registry.GetPipeline("payment-api");
        var resolvedCaseInsensitive = registry.GetPipeline("PAYMENT-API");

        // Assert
        returned.Should().BeSameAs(registry);
        resolved.Should().BeSameAs(pipeline);
        resolvedCaseInsensitive.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void Register_WithInvalidArguments_ThrowsExceptions()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline("valid");

        // Act & Assert
        var act1 = () => registry.Register(null!, pipeline);
        var act2 = () => registry.Register(string.Empty, pipeline);
        var act3 = () => registry.Register("   ", pipeline);
        var act4 = () => registry.Register("valid", null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
        act4.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetPipeline_WithInvalidName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();

        // Act & Assert
        var act1 = () => registry.GetPipeline(null!);
        var act2 = () => registry.GetPipeline(string.Empty);
        var act3 = () => registry.GetPipeline("   ");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPipeline_WhenNotRegistered_ThrowsResiliencePolicyNotFoundException()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();

        // Act
        var act = () => registry.GetPipeline("non-existent");

        // Assert
        act.Should().Throw<ResiliencePolicyNotFoundException>()
            .Which.PolicyName.Should().Be("non-existent");
    }

    [Fact]
    public void TryGetPipeline_WhenPresent_ReturnsTrueAndPipeline()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline("inventory-api");
        registry.Register("inventory-api", pipeline);

        // Act
        var found = registry.TryGetPipeline("inventory-api", out var resolved);

        // Assert
        found.Should().BeTrue();
        resolved.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void TryGetPipeline_WhenMissing_ReturnsFalseAndNull()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();

        // Act
        var found = registry.TryGetPipeline("missing-api", out var resolved);

        // Assert
        found.Should().BeFalse();
        resolved.Should().BeNull();
    }

    [Fact]
    public void TryGetPipeline_WithInvalidName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();

        // Act & Assert
        var act1 = () => registry.TryGetPipeline(null!, out _);
        var act2 = () => registry.TryGetPipeline(string.Empty, out _);
        var act3 = () => registry.TryGetPipeline("   ", out _);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterTyped_And_GetPipelineTyped_ReturnsRegisteredInstance()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline<string>("typed-api");

        // Act
        var returned = registry.Register("typed-api", pipeline);
        var resolved = registry.GetPipeline<string>("typed-api");
        var resolvedCaseInsensitive = registry.GetPipeline<string>("TYPED-API");

        // Assert
        returned.Should().BeSameAs(registry);
        resolved.Should().BeSameAs(pipeline);
        resolvedCaseInsensitive.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void RegisterTyped_WithInvalidArguments_ThrowsExceptions()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline<int>("valid");

        // Act & Assert
        var act1 = () => registry.Register<int>(null!, pipeline);
        var act2 = () => registry.Register<int>(string.Empty, pipeline);
        var act3 = () => registry.Register<int>("   ", pipeline);
        var act4 = () => registry.Register<int>("valid", null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
        act4.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetPipelineTyped_WithInvalidNameOrMissing_ThrowsExceptions()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();

        // Act & Assert
        var act1 = () => registry.GetPipeline<int>(null!);
        var act2 = () => registry.GetPipeline<int>(string.Empty);
        var act3 = () => registry.GetPipeline<int>("   ");
        var act4 = () => registry.GetPipeline<int>("missing");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
        act4.Should().Throw<ResiliencePolicyNotFoundException>()
            .Which.PolicyName.Should().Be("missing");
    }

    [Fact]
    public void TryGetPipelineTyped_WhenPresent_ReturnsTrueAndPipeline()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline<double>("math-api");
        registry.Register("math-api", pipeline);

        // Act
        var found = registry.TryGetPipeline<double>("math-api", out var resolved);

        // Assert
        found.Should().BeTrue();
        resolved.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void TryGetPipelineTyped_WhenMissingOrTypeMismatch_ReturnsFalseAndNull()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var pipeline = new PassthroughResiliencePipeline<string>("string-api");
        registry.Register("string-api", pipeline);

        // Act
        var foundMissing = registry.TryGetPipeline<int>("missing-api", out var resolvedMissing);
        var foundMismatch = registry.TryGetPipeline<int>("string-api", out var resolvedMismatch);

        // Assert
        foundMissing.Should().BeFalse();
        resolvedMissing.Should().BeNull();
        foundMismatch.Should().BeFalse();
        resolvedMismatch.Should().BeNull();
    }

    [Fact]
    public void TryGetPipelineTyped_WithInvalidName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();

        // Act & Assert
        var act1 = () => registry.TryGetPipeline<int>(null!, out _);
        var act2 = () => registry.TryGetPipeline<int>(string.Empty, out _);
        var act3 = () => registry.TryGetPipeline<int>("   ", out _);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }
}
