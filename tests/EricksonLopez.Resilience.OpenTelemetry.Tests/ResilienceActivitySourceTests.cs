// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using AwesomeAssertions;
using EricksonLopez.Resilience.OpenTelemetry;
using Xunit;

namespace EricksonLopez.Resilience.OpenTelemetry.Tests;

public sealed class ResilienceActivitySourceTests
{
    [Fact]
    public void ResilienceActivitySource_ExposesSourceNameAndVersion()
    {
        // Assert
        ResilienceActivitySource.SourceName.Should().Be("EricksonLopez.Resilience");
        ResilienceActivitySource.SourceVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void StartExecutionActivity_WithListener_AttachesAllTags()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ResilienceActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = act => activities.Add(act)
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ResilienceActivitySource.StartExecutionActivity(
            policyName: "my-policy",
            operationName: "execute-query",
            tenantId: "tenant-42",
            correlationId: "corr-99");

        // Assert
        activity.Should().NotBeNull();
        activity!.OperationName.Should().Be("Resilience.Execute");
        activity.Kind.Should().Be(ActivityKind.Internal);

        activity.GetTagItem("resilience.policy").Should().Be("my-policy");
        activity.GetTagItem("resilience.operation").Should().Be("execute-query");
        activity.GetTagItem("resilience.tenant_id").Should().Be("tenant-42");
        activity.GetTagItem("resilience.correlation_id").Should().Be("corr-99");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    public void StartExecutionActivity_WithNullOrEmptyTenantAndCorrelation_OmitsOptionalTags(string? tenantId, string? correlationId)
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ResilienceActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var activity = ResilienceActivitySource.StartExecutionActivity(
            policyName: "retry-policy",
            operationName: "save-entity",
            tenantId: tenantId,
            correlationId: correlationId);

        // Assert
        activity.Should().NotBeNull();
        activity!.GetTagItem("resilience.policy").Should().Be("retry-policy");
        activity.GetTagItem("resilience.operation").Should().Be("save-entity");
        activity.GetTagItem("resilience.tenant_id").Should().BeNull();
        activity.GetTagItem("resilience.correlation_id").Should().BeNull();
    }
}
