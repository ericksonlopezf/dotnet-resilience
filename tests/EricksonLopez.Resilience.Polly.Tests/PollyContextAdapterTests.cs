// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.Polly.Adapters;
using global::Polly;
using Xunit;

namespace EricksonLopez.Resilience.Polly.Tests;

public sealed class PollyContextAdapterTests
{
    [Fact]
    public void ToPollyContext_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => PollyContextAdapter.ToPollyContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetEcosystemContext_WithNullContext_ThrowsArgumentNullException()
    {
        var act = () => PollyContextAdapter.GetEcosystemContext(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToPollyContext_MapsPropertiesAndRetrievesOriginalInstance()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ecoContext = ResilienceContext.Create("sample-policy", cts.Token)
            .WithOperationName("ProcessPayment")
            .WithCorrelationId("corr-777")
            .WithTenantId("tenant-xyz")
            .SetProperty("my-key", 42)
            .SetProperty("app-name", "Payments");

        // Act
        var pollyContext = PollyContextAdapter.ToPollyContext(ecoContext);

        // Assert
        pollyContext.OperationKey.Should().Be("ProcessPayment");
        pollyContext.CancellationToken.Should().Be(cts.Token);

        var key = new ResiliencePropertyKey<ResilienceContext>("EricksonLopez.Resilience.EcosystemContext");
        pollyContext.Properties.TryGetValue(key, out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(ecoContext);

        var customPropKey = new ResiliencePropertyKey<object?>("my-key");
        pollyContext.Properties.TryGetValue(customPropKey, out var customVal).Should().BeTrue();
        customVal.Should().Be(42);

        var appPropKey = new ResiliencePropertyKey<object?>("app-name");
        pollyContext.Properties.TryGetValue(appPropKey, out var appVal).Should().BeTrue();
        appVal.Should().Be("Payments");

        var extractedContext = PollyContextAdapter.GetEcosystemContext(pollyContext);
        extractedContext.Should().NotBeNull();
        extractedContext.Should().BeSameAs(ecoContext);
        extractedContext!.PolicyName.Should().Be("sample-policy");
        extractedContext.OperationName.Should().Be("ProcessPayment");
        extractedContext.CorrelationId.Should().Be("corr-777");
        extractedContext.TenantId.Should().Be("tenant-xyz");

        // Act & Assert Return
        PollyContextAdapter.Return(pollyContext);
        pollyContext.Properties.TryGetValue(key, out _).Should().BeFalse();
    }

    [Fact]
    public void GetEcosystemContext_WhenPropertyNotPresent_ReturnsNull()
    {
        // Arrange
        var pollyContext = global::Polly.ResilienceContextPool.Shared.Get();

        try
        {
            // Act
            var extracted = PollyContextAdapter.GetEcosystemContext(pollyContext);

            // Assert
            extracted.Should().BeNull();
        }
        finally
        {
            PollyContextAdapter.Return(pollyContext);
        }
    }

    [Fact]
    public void Return_WithNullContext_DoesNotThrow()
    {
        var act = () => PollyContextAdapter.Return(null!);
        act.Should().NotThrow();
    }
}
