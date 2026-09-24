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

    [Fact]
    public void RecordException_WhenActivityOrExceptionIsNull_DoesNotThrow()
    {
        // Act & Assert
        Action act1 = () => ResilienceActivitySource.RecordException(null, new InvalidOperationException("Boom"));
        act1.Should().NotThrow();

        using var activity = new Activity("TestActivity");
        Action act2 = () => ResilienceActivitySource.RecordException(activity, null!);
        act2.Should().NotThrow();
        activity.Status.Should().Be(ActivityStatusCode.Unset);
        activity.Events.Should().BeEmpty();
    }

    [Fact]
    public void RecordException_WithoutSanitizer_RecordsExceptionDetailsAndStatus()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ResilienceActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ResilienceActivitySource.StartExecutionActivity("error-policy", "execute-op");
        activity.Should().NotBeNull();

        var exception = new InvalidOperationException("Fatal database deadlock");

        // Act
        ResilienceActivitySource.RecordException(activity, exception);

        // Assert
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Fatal database deadlock");

        var evt = activity.Events.Should().ContainSingle().Subject;
        evt.Name.Should().Be("exception");

        var tagsDict = new Dictionary<string, object?>();
        foreach (var tag in evt.Tags)
        {
            tagsDict[tag.Key] = tag.Value;
        }

        tagsDict["exception.type"].Should().Be(typeof(InvalidOperationException).FullName);
        tagsDict["exception.message"].Should().Be("Fatal database deadlock");
        tagsDict["exception.stacktrace"].Should().NotBeNull();
    }

    [Fact]
    public void RecordException_WithCustomExceptionSanitizer_AppliesSanitization()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ResilienceActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ResilienceActivitySource.StartExecutionActivity("sanitized-policy", "secure-op");
        activity.Should().NotBeNull();

        var exception = new ArgumentException("Sensitive token: 12345");

        try
        {
            ResilienceActivitySource.ExceptionSanitizer = ex => ($"[REDACTED] {ex.GetType().Name}", "SafeStack");

            // Act
            ResilienceActivitySource.RecordException(activity, exception);

            // Assert
            activity!.Status.Should().Be(ActivityStatusCode.Error);
            activity.StatusDescription.Should().Be("[REDACTED] ArgumentException");

            var evt = activity.Events.Should().ContainSingle().Subject;
            var tagsDict = new Dictionary<string, object?>();
            foreach (var tag in evt.Tags)
            {
                tagsDict[tag.Key] = tag.Value;
            }

            tagsDict["exception.message"].Should().Be("[REDACTED] ArgumentException");
            tagsDict["exception.stacktrace"].Should().Be("SafeStack");
        }
        finally
        {
            ResilienceActivitySource.ExceptionSanitizer = null;
        }
    }
}
