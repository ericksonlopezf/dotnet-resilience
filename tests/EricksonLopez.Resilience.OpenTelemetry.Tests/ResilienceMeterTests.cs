// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.Options;
using Xunit;

namespace EricksonLopez.Resilience.OpenTelemetry.Tests;

[Collection("OpenTelemetryTests")]
public sealed class ResilienceMeterTests
{
    private sealed class CapturedMeasurement<T>
    {
        public string InstrumentName { get; init; } = string.Empty;
        public T Value { get; init; } = default!;
        public Dictionary<string, object?> Tags { get; init; } = new();
    }

    [Fact]
    public void ResilienceMeter_ExposesSemanticMeterAndVersion()
    {
        // Assert
        ResilienceMeter.MeterName.Should().Be("EricksonLopez.Resilience");
        ResilienceMeter.MeterVersion.Should().Be("1.0.0");
    }

    [Fact]
    public void ResilienceMeter_Instruments_ExposeSemanticUnitsAndDescriptions()
    {
        var instruments = new Dictionary<string, (string? Unit, string? Description)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                instruments[instrument.Name] = (instrument.Unit, instrument.Description);
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.Start();

        instruments.Should().ContainKey("resilience.execution.duration");
        instruments["resilience.execution.duration"].Should().Be(("ms", "Execution duration of resilient operations in milliseconds."));

        instruments.Should().ContainKey("resilience.retry.attempts");
        instruments["resilience.retry.attempts"].Should().Be(("{attempt}", "Number of retry attempts executed."));

        instruments.Should().ContainKey("resilience.circuit_breaker.state_changes");
        instruments["resilience.circuit_breaker.state_changes"].Should().Be(("{transition}", "Number of circuit breaker state transitions."));

        instruments.Should().ContainKey("resilience.timeout.rejections");
        instruments["resilience.timeout.rejections"].Should().Be(("{rejection}", "Number of operations terminated due to timeout."));

        instruments.Should().ContainKey("resilience.rate_limiter.rejections");
        instruments["resilience.rate_limiter.rejections"].Should().Be(("{rejection}", "Number of operations rejected due to rate limiting."));
    }

    [Fact]
    public void RecordExecution_SuccessWithTenant_RecordsHistogramMeasurementAndTags()
    {
        // Arrange
        var captured = new List<CapturedMeasurement<double>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            captured.Add(new CapturedMeasurement<double>
            {
                InstrumentName = instrument.Name,
                Value = measurement,
                Tags = dict
            });
        });
        meterListener.Start();

        // Act
        ResilienceMeter.RecordExecution("payment-policy", "Charge", 150.5, isSuccess: true, tenantId: "tenant-A");

        // Assert
        meterListener.RecordObservableInstruments();
        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.execution.duration" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "payment-policy"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(150.5);
        item.Tags["resilience.policy"].Should().Be("payment-policy");
        item.Tags["resilience.operation"].Should().Be("Charge");
        item.Tags["resilience.status"].Should().Be("success");
        item.Tags["resilience.tenant_id"].Should().Be("tenant-A");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RecordExecution_FailureWithoutTenant_RecordsHistogramMeasurementAndFailureStatus(string? tenantId)
    {
        // Arrange
        var captured = new List<CapturedMeasurement<double>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            captured.Add(new CapturedMeasurement<double>
            {
                InstrumentName = instrument.Name,
                Value = measurement,
                Tags = dict
            });
        });
        meterListener.Start();

        // Act
        ResilienceMeter.RecordExecution("order-policy", "PlaceOrder", 45.0, isSuccess: false, tenantId: tenantId);

        // Assert
        meterListener.RecordObservableInstruments();
        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.execution.duration" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "order-policy"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(45.0);
        item.Tags["resilience.policy"].Should().Be("order-policy");
        item.Tags["resilience.operation"].Should().Be("PlaceOrder");
        item.Tags["resilience.status"].Should().Be("failure");
        item.Tags.ContainsKey("resilience.tenant_id").Should().BeFalse();
    }

    [Theory]
    [InlineData("tenant-100", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void RecordRetry_RecordsCounterMeasurementAndTags(string? tenantId, bool hasTenantTag)
    {
        // Arrange
        var captured = new List<CapturedMeasurement<long>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            captured.Add(new CapturedMeasurement<long>
            {
                InstrumentName = instrument.Name,
                Value = measurement,
                Tags = dict
            });
        });
        meterListener.Start();

        // Act
        ResilienceMeter.RecordRetry("retry-policy", "ProcessMsg", attemptNumber: 3, tenantId: tenantId);

        // Assert
        meterListener.RecordObservableInstruments();
        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.retry.attempts" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "retry-policy"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(1);
        item.Tags["resilience.policy"].Should().Be("retry-policy");
        item.Tags["resilience.operation"].Should().Be("ProcessMsg");
        item.Tags["resilience.attempt"].Should().Be(3);

        if (hasTenantTag)
        {
            item.Tags["resilience.tenant_id"].Should().Be(tenantId);
        }
        else
        {
            item.Tags.ContainsKey("resilience.tenant_id").Should().BeFalse();
        }
    }

    [Theory]
    [InlineData(CircuitBreakerState.Open, "tenant-CB", true)]
    [InlineData(CircuitBreakerState.Closed, null, false)]
    [InlineData(CircuitBreakerState.HalfOpen, "", false)]
    public void RecordCircuitStateChange_RecordsCounterMeasurementAndTags(CircuitBreakerState state, string? tenantId, bool hasTenantTag)
    {
        // Arrange
        var captured = new List<CapturedMeasurement<long>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            captured.Add(new CapturedMeasurement<long>
            {
                InstrumentName = instrument.Name,
                Value = measurement,
                Tags = dict
            });
        });
        meterListener.Start();

        // Act
        ResilienceMeter.RecordCircuitStateChange("cb-policy", state, tenantId: tenantId);

        // Assert
        meterListener.RecordObservableInstruments();
        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.circuit_breaker.state_changes" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "cb-policy"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(1);
        item.Tags["resilience.policy"].Should().Be("cb-policy");
        item.Tags["resilience.circuit.state"].Should().Be(state.ToString());

        if (hasTenantTag)
        {
            item.Tags["resilience.tenant_id"].Should().Be(tenantId);
        }
        else
        {
            item.Tags.ContainsKey("resilience.tenant_id").Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("tenant-T", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void RecordTimeout_RecordsCounterMeasurementAndTags(string? tenantId, bool hasTenantTag)
    {
        // Arrange
        var captured = new List<CapturedMeasurement<long>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            captured.Add(new CapturedMeasurement<long>
            {
                InstrumentName = instrument.Name,
                Value = measurement,
                Tags = dict
            });
        });
        meterListener.Start();

        // Act
        ResilienceMeter.RecordTimeout("timeout-policy", "FetchRemote", tenantId: tenantId);

        // Assert
        meterListener.RecordObservableInstruments();
        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.timeout.rejections" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "timeout-policy"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(1);
        item.Tags["resilience.policy"].Should().Be("timeout-policy");
        item.Tags["resilience.operation"].Should().Be("FetchRemote");

        if (hasTenantTag)
        {
            item.Tags["resilience.tenant_id"].Should().Be(tenantId);
        }
        else
        {
            item.Tags.ContainsKey("resilience.tenant_id").Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("tenant-RL", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void RecordRateLimitRejection_RecordsCounterMeasurementAndTags(string? tenantId, bool hasTenantTag)
    {
        // Arrange
        var captured = new List<CapturedMeasurement<long>>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ResilienceMeter.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }
            captured.Add(new CapturedMeasurement<long>
            {
                InstrumentName = instrument.Name,
                Value = measurement,
                Tags = dict
            });
        });
        meterListener.Start();

        // Act
        ResilienceMeter.RecordRateLimitRejection("rl-policy", "CallApi", tenantId: tenantId);

        // Assert
        meterListener.RecordObservableInstruments();
        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.rate_limiter.rejections" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "rl-policy"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(1);
        item.Tags["resilience.policy"].Should().Be("rl-policy");
        item.Tags["resilience.operation"].Should().Be("CallApi");

        if (hasTenantTag)
        {
            item.Tags["resilience.tenant_id"].Should().Be(tenantId);
        }
        else
        {
            item.Tags.ContainsKey("resilience.tenant_id").Should().BeFalse();
        }
    }
}
