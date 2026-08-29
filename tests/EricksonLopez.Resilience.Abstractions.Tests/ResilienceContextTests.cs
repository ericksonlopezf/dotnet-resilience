// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.Resilience;
using Xunit;

namespace EricksonLopez.Resilience.Abstractions.Tests;

public sealed class ResilienceContextTests
{
    [Fact]
    public void Create_WithPolicyName_InitializesCorrectly()
    {
        // Act
        var context = ResilienceContext.Create("test-policy");

        // Assert
        context.PolicyName.Should().Be("test-policy");
        context.OperationName.Should().Be("test-policy");
        context.CorrelationId.Should().BeNull();
        context.TenantId.Should().BeNull();
        context.AttemptNumber.Should().Be(1);
        context.CancellationToken.Should().Be(CancellationToken.None);
        context.Properties.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var context = ResilienceContext.Create("test-policy", cts.Token);

        // Assert
        context.CancellationToken.Should().Be(cts.Token);
        context.PolicyName.Should().Be("test-policy");
        context.OperationName.Should().Be("test-policy");
    }

    [Fact]
    public void Constructor_WithExplicitParameters_InitializesAllProperties()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var context = new ResilienceContext(
            policyName: "custom-policy",
            operationName: "custom-op",
            correlationId: "corr-123",
            tenantId: "tenant-abc",
            cancellationToken: cts.Token);

        // Assert
        context.PolicyName.Should().Be("custom-policy");
        context.OperationName.Should().Be("custom-op");
        context.CorrelationId.Should().Be("corr-123");
        context.TenantId.Should().Be("tenant-abc");
        context.CancellationToken.Should().Be(cts.Token);
        context.AttemptNumber.Should().Be(1);
    }

    [Fact]
    public void Constructor_WithNullOrEmptyPolicyName_ThrowsArgumentException()
    {
        // Act & Assert
        var act1 = () => new ResilienceContext(null!);
        var act2 = () => new ResilienceContext(string.Empty);
        var act3 = () => new ResilienceContext("   ");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AttemptNumber_CanBeMutatedAndRetrieved()
    {
        // Arrange
        var context = ResilienceContext.Create("policy1");

        // Act
        context.AttemptNumber = 42;

        // Assert
        context.AttemptNumber.Should().Be(42);
    }

    [Fact]
    public void SetAndGetProperty_StoresAndRetrievesValueCorrectly()
    {
        // Arrange
        var context = ResilienceContext.Create("test-policy");
        var guid = Guid.NewGuid();

        // Act
        var returned = context.SetProperty("custom_key", "custom_value");
        context.SetProperty("guid_key", guid);

        // Assert
        returned.Should().BeSameAs(context);
        context.TryGetProperty<string>("custom_key", out var strVal).Should().BeTrue();
        strVal.Should().Be("custom_value");

        context.TryGetProperty<Guid>("guid_key", out var guidVal).Should().BeTrue();
        guidVal.Should().Be(guid);

        context.TryGetProperty<int>("non_existing", out var defaultVal).Should().BeFalse();
        defaultVal.Should().Be(0);
    }

    [Fact]
    public void SetProperty_WithCaseInsensitiveKeys_RetrievesSuccessfully()
    {
        // Arrange
        var context = ResilienceContext.Create("test-policy");

        // Act
        context.SetProperty("CamelCaseKey", 999);

        // Assert
        context.TryGetProperty<int>("camelcasekey", out var val1).Should().BeTrue();
        val1.Should().Be(999);

        context.TryGetProperty<int>("CAMELCASEKEY", out var val2).Should().BeTrue();
        val2.Should().Be(999);
    }

    [Fact]
    public void SetProperty_WithInvalidKey_ThrowsArgumentException()
    {
        // Arrange
        var context = ResilienceContext.Create("policy");

        // Act & Assert
        var act1 = () => context.SetProperty(null!, "val");
        var act2 = () => context.SetProperty(string.Empty, "val");
        var act3 = () => context.SetProperty("   ", "val");

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetProperty_WithInvalidKey_ThrowsArgumentException()
    {
        // Arrange
        var context = ResilienceContext.Create("policy");

        // Act & Assert
        var act1 = () => context.TryGetProperty<string>(null!, out _);
        var act2 = () => context.TryGetProperty<string>(string.Empty, out _);
        var act3 = () => context.TryGetProperty<string>("   ", out _);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentException>();
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryGetProperty_WithTypeMismatch_ReturnsFalseAndDefault()
    {
        // Arrange
        var context = ResilienceContext.Create("policy");
        context.SetProperty("stringKey", "not an integer");

        // Act
        var result = context.TryGetProperty<int>("stringKey", out var intVal);

        // Assert
        result.Should().BeFalse();
        intVal.Should().Be(0);
    }

    [Fact]
    public void TryGetProperty_WithNullValueStored_ReturnsFalse()
    {
        // Arrange
        var context = ResilienceContext.Create("policy");
        context.SetProperty("nullKey", null);

        // Act
        var result = context.TryGetProperty<string>("nullKey", out var strVal);

        // Assert
        result.Should().BeFalse();
        strVal.Should().BeNull();
    }

    [Fact]
    public void FluentWithMethods_CreatesNewInstancesWithModifiedProperties()
    {
        // Arrange
        var original = ResilienceContext.Create("policy1")
            .SetProperty("meta", "data");
        original.AttemptNumber = 2;

        // Act
        var modified = original
            .WithOperationName("op1")
            .WithCorrelationId("corr1")
            .WithTenantId("tenant1")
            .WithAttemptNumber(3);

        // Assert
        modified.Should().NotBeSameAs(original);
        modified.PolicyName.Should().Be("policy1");
        modified.OperationName.Should().Be("op1");
        modified.CorrelationId.Should().Be("corr1");
        modified.TenantId.Should().Be("tenant1");
        modified.AttemptNumber.Should().Be(3);
        modified.TryGetProperty<string>("meta", out var val).Should().BeTrue();
        val.Should().Be("data");
    }

    [Fact]
    public void WithOperationName_PreservesAllExistingStateAndProperties()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var original = new ResilienceContext("pol", "initialOp", "corr", "tenant", cts.Token)
        {
            AttemptNumber = 5
        };
        original.SetProperty("p1", "v1");

        // Act
        var next = original.WithOperationName("newOp");

        // Assert
        next.Should().NotBeSameAs(original);
        next.PolicyName.Should().Be("pol");
        next.OperationName.Should().Be("newOp");
        next.CorrelationId.Should().Be("corr");
        next.TenantId.Should().Be("tenant");
        next.CancellationToken.Should().Be(cts.Token);
        next.AttemptNumber.Should().Be(5);
        next.TryGetProperty<string>("p1", out var p1Val).Should().BeTrue();
        p1Val.Should().Be("v1");
    }

    [Fact]
    public void WithCorrelationId_PreservesAllExistingStateAndProperties()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var original = new ResilienceContext("pol", "op", "initialCorr", "tenant", cts.Token)
        {
            AttemptNumber = 4
        };
        original.SetProperty("p2", 123);

        // Act
        var next = original.WithCorrelationId("newCorr");

        // Assert
        next.Should().NotBeSameAs(original);
        next.PolicyName.Should().Be("pol");
        next.OperationName.Should().Be("op");
        next.CorrelationId.Should().Be("newCorr");
        next.TenantId.Should().Be("tenant");
        next.CancellationToken.Should().Be(cts.Token);
        next.AttemptNumber.Should().Be(4);
        next.TryGetProperty<int>("p2", out var p2Val).Should().BeTrue();
        p2Val.Should().Be(123);
    }

    [Fact]
    public void WithTenantId_PreservesAllExistingStateAndProperties()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var original = new ResilienceContext("pol", "op", "corr", "initialTenant", cts.Token)
        {
            AttemptNumber = 7
        };
        original.SetProperty("p3", true);

        // Act
        var next = original.WithTenantId("newTenant");

        // Assert
        next.Should().NotBeSameAs(original);
        next.PolicyName.Should().Be("pol");
        next.OperationName.Should().Be("op");
        next.CorrelationId.Should().Be("corr");
        next.TenantId.Should().Be("newTenant");
        next.CancellationToken.Should().Be(cts.Token);
        next.AttemptNumber.Should().Be(7);
        next.TryGetProperty<bool>("p3", out var p3Val).Should().BeTrue();
        p3Val.Should().BeTrue();
    }

    [Fact]
    public void WithAttemptNumber_PreservesAllExistingStateAndProperties()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var original = new ResilienceContext("pol", "op", "corr", "tenant", cts.Token)
        {
            AttemptNumber = 1
        };
        original.SetProperty("p4", 9.99);

        // Act
        var next = original.WithAttemptNumber(10);

        // Assert
        next.Should().NotBeSameAs(original);
        next.PolicyName.Should().Be("pol");
        next.OperationName.Should().Be("op");
        next.CorrelationId.Should().Be("corr");
        next.TenantId.Should().Be("tenant");
        next.CancellationToken.Should().Be(cts.Token);
        next.AttemptNumber.Should().Be(10);
        next.TryGetProperty<double>("p4", out var p4Val).Should().BeTrue();
        p4Val.Should().Be(9.99);
    }
}
