// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.OpenTelemetry.Extensions;
using EricksonLopez.Resilience.Options;
using Xunit;

namespace EricksonLopez.Resilience.OpenTelemetry.Tests;

[Collection("OpenTelemetryTests")]
public sealed class OpenTelemetryResilienceExtensionsTests
{
    private sealed class CapturedMeasurement<T>
    {
        public string InstrumentName { get; init; } = string.Empty;
        public T Value { get; init; } = default!;
        public Dictionary<string, object?> Tags { get; init; } = new();
    }

    #region RetryStrategyOptions

    [Fact]
    public void WithTelemetry_RetryOptions_NullOptions_ThrowsArgumentNullException()
    {
        RetryStrategyOptions? options = null;
        Action act = () => options!.WithTelemetry();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task WithTelemetry_RetryOptions_ChainsAndExecutesTelemetryAndOriginalDelegate()
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

        var originalCalled = false;
        var options = new RetryStrategyOptions
        {
            OnRetry = ctx =>
            {
                originalCalled = true;
                return ValueTask.CompletedTask;
            }
        };

        // Act
        var returned = options.WithTelemetry();
        returned.Should().BeSameAs(options);

        var resilienceContext = new ResilienceContext("retry-pol", "retry-op", tenantId: "tenant-99");
        var onRetryContext = new RetryAttemptContext(resilienceContext, attemptNumber: 2, delay: TimeSpan.FromSeconds(1), exception: new InvalidOperationException("Fail"));

        await options.OnRetry!(onRetryContext);

        // Assert
        originalCalled.Should().BeTrue();
        meterListener.RecordObservableInstruments();

        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.retry.attempts" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "retry-pol"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(1);
        item.Tags["resilience.policy"].Should().Be("retry-pol");
        item.Tags["resilience.operation"].Should().Be("retry-op");
        item.Tags["resilience.attempt"].Should().Be(2);
        item.Tags["resilience.tenant_id"].Should().Be("tenant-99");
    }

    [Fact]
    public async Task WithTelemetry_RetryOptions_WithoutOriginalDelegate_ExecutesWithoutError()
    {
        // Arrange
        var options = new RetryStrategyOptions();
        options.WithTelemetry();

        var resilienceContext = new ResilienceContext("retry-pol", "retry-op");
        var onRetryContext = new RetryAttemptContext(resilienceContext, attemptNumber: 1, delay: TimeSpan.FromSeconds(1), exception: null);

        // Act
        Func<Task> act = async () => await options.OnRetry!(onRetryContext);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region CircuitBreakerStrategyOptions

    [Fact]
    public void WithTelemetry_CircuitBreakerOptions_NullOptions_ThrowsArgumentNullException()
    {
        CircuitBreakerStrategyOptions? options = null;
        Action act = () => options!.WithTelemetry();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task WithTelemetry_CircuitBreakerOptions_OnCircuitOpened_ExecutesTelemetryAndOriginalDelegate()
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

        var originalOpenedCalled = false;
        var options = new CircuitBreakerStrategyOptions
        {
            Name = "CB-Name",
            OnCircuitOpened = ctx =>
            {
                originalOpenedCalled = true;
                return ValueTask.CompletedTask;
            }
        };

        options.WithTelemetry();

        // 1. With ResilienceContext
        var resCtx = new ResilienceContext("cb-policy", "cb-op", tenantId: "tenant-cb");
        var ctx1 = new CircuitBreakerStateContext(resCtx, CircuitBreakerState.Open, breakDuration: TimeSpan.FromSeconds(10));
        await options.OnCircuitOpened!(ctx1);

        originalOpenedCalled.Should().BeTrue();
        meterListener.RecordObservableInstruments();

        var item1 = captured.Last();
        item1.Tags["resilience.policy"].Should().Be("cb-policy");
        item1.Tags["resilience.circuit.state"].Should().Be(CircuitBreakerState.Open.ToString());
        item1.Tags["resilience.tenant_id"].Should().Be("tenant-cb");

        // 2. With null ResilienceContext and configured Name
        var ctx2 = new CircuitBreakerStateContext(resilienceContext: null, CircuitBreakerState.Open, breakDuration: TimeSpan.FromSeconds(10));
        await options.OnCircuitOpened(ctx2);

        var item2 = captured.Last();
        item2.Tags["resilience.policy"].Should().Be("CB-Name");
        item2.Tags.ContainsKey("resilience.tenant_id").Should().BeFalse();

        // 3. With null ResilienceContext and null Name
        options.Name = null;
        await options.OnCircuitOpened(ctx2);

        var item3 = captured.Last();
        item3.Tags["resilience.policy"].Should().Be("CircuitBreaker");
    }

    [Fact]
    public async Task WithTelemetry_CircuitBreakerOptions_OnCircuitClosed_ExecutesTelemetryAndOriginalDelegate()
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

        var originalClosedCalled = false;
        var options = new CircuitBreakerStrategyOptions
        {
            Name = "CB-Closed-Name",
            OnCircuitClosed = ctx =>
            {
                originalClosedCalled = true;
                return ValueTask.CompletedTask;
            }
        };

        options.WithTelemetry();

        // 1. With ResilienceContext
        var resCtx = new ResilienceContext("cb-closed-policy", "cb-op", tenantId: "tenant-closed");
        var ctx1 = new CircuitBreakerStateContext(resCtx, CircuitBreakerState.Closed);
        await options.OnCircuitClosed!(ctx1);

        originalClosedCalled.Should().BeTrue();
        meterListener.RecordObservableInstruments();

        var item1 = captured.Last();
        item1.Tags["resilience.policy"].Should().Be("cb-closed-policy");
        item1.Tags["resilience.circuit.state"].Should().Be(CircuitBreakerState.Closed.ToString());
        item1.Tags["resilience.tenant_id"].Should().Be("tenant-closed");

        // 2. With null ResilienceContext and configured Name
        var ctx2 = new CircuitBreakerStateContext(resilienceContext: null, CircuitBreakerState.Closed);
        await options.OnCircuitClosed(ctx2);

        var item2 = captured.Last();
        item2.Tags["resilience.policy"].Should().Be("CB-Closed-Name");

        // 3. With null ResilienceContext and null Name
        options.Name = null;
        await options.OnCircuitClosed(ctx2);

        var item3 = captured.Last();
        item3.Tags["resilience.policy"].Should().Be("CircuitBreaker");
    }

    [Fact]
    public async Task WithTelemetry_CircuitBreakerOptions_OnCircuitHalfOpened_ExecutesTelemetryAndOriginalDelegate()
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

        var originalHalfOpenedCalled = false;
        var options = new CircuitBreakerStrategyOptions
        {
            Name = "CB-Half-Name",
            OnCircuitHalfOpened = ctx =>
            {
                originalHalfOpenedCalled = true;
                return ValueTask.CompletedTask;
            }
        };

        options.WithTelemetry();

        // 1. With ResilienceContext
        var resCtx = new ResilienceContext("cb-half-policy", "cb-op", tenantId: "tenant-half");
        var ctx1 = new CircuitBreakerStateContext(resCtx, CircuitBreakerState.HalfOpen);
        await options.OnCircuitHalfOpened!(ctx1);

        originalHalfOpenedCalled.Should().BeTrue();
        meterListener.RecordObservableInstruments();

        var item1 = captured.Last();
        item1.Tags["resilience.policy"].Should().Be("cb-half-policy");
        item1.Tags["resilience.circuit.state"].Should().Be(CircuitBreakerState.HalfOpen.ToString());
        item1.Tags["resilience.tenant_id"].Should().Be("tenant-half");

        // 2. With null ResilienceContext and configured Name
        var ctx2 = new CircuitBreakerStateContext(resilienceContext: null, CircuitBreakerState.HalfOpen);
        await options.OnCircuitHalfOpened(ctx2);

        var item2 = captured.Last();
        item2.Tags["resilience.policy"].Should().Be("CB-Half-Name");

        // 3. With null ResilienceContext and null Name
        options.Name = null;
        await options.OnCircuitHalfOpened(ctx2);

        var item3 = captured.Last();
        item3.Tags["resilience.policy"].Should().Be("CircuitBreaker");
    }

    [Fact]
    public async Task WithTelemetry_CircuitBreakerOptions_WithoutOriginalDelegates_ExecutesWithoutError()
    {
        // Arrange
        var options = new CircuitBreakerStrategyOptions();
        options.WithTelemetry();

        var ctx = new CircuitBreakerStateContext(new ResilienceContext("p", "o"), CircuitBreakerState.Open, TimeSpan.FromSeconds(5));

        // Act & Assert
        Func<Task> act1 = async () => await options.OnCircuitOpened!(ctx);
        Func<Task> act2 = async () => await options.OnCircuitClosed!(new CircuitBreakerStateContext(null, CircuitBreakerState.Closed));
        Func<Task> act3 = async () => await options.OnCircuitHalfOpened!(new CircuitBreakerStateContext(null, CircuitBreakerState.HalfOpen));

        await act1.Should().NotThrowAsync();
        await act2.Should().NotThrowAsync();
        await act3.Should().NotThrowAsync();
    }

    #endregion

    #region TimeoutStrategyOptions

    [Fact]
    public void WithTelemetry_TimeoutOptions_NullOptions_ThrowsArgumentNullException()
    {
        TimeoutStrategyOptions? options = null;
        Action act = () => options!.WithTelemetry();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task WithTelemetry_TimeoutOptions_ChainsAndExecutesTelemetryAndOriginalDelegate()
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

        var originalCalled = false;
        var options = new TimeoutStrategyOptions
        {
            OnTimeout = ctx =>
            {
                originalCalled = true;
                return ValueTask.CompletedTask;
            }
        };

        // Act
        var returned = options.WithTelemetry();
        returned.Should().BeSameAs(options);

        var resilienceContext = new ResilienceContext("timeout-pol", "timeout-op", tenantId: "tenant-timeout");
        var onTimeoutContext = new TimeoutContext(resilienceContext, timeout: TimeSpan.FromSeconds(5));

        await options.OnTimeout!(onTimeoutContext);

        // Assert
        originalCalled.Should().BeTrue();
        meterListener.RecordObservableInstruments();

        var item = captured.FirstOrDefault(c => c.InstrumentName == "resilience.timeout.rejections" && Equals(c.Tags.GetValueOrDefault("resilience.policy"), "timeout-pol"));
        item.Should().NotBeNull();
        item!.Value.Should().Be(1);
        item.Tags["resilience.policy"].Should().Be("timeout-pol");
        item.Tags["resilience.operation"].Should().Be("timeout-op");
        item.Tags["resilience.tenant_id"].Should().Be("tenant-timeout");
    }

    [Fact]
    public async Task WithTelemetry_TimeoutOptions_WithoutOriginalDelegate_ExecutesWithoutError()
    {
        // Arrange
        var options = new TimeoutStrategyOptions();
        options.WithTelemetry();

        var resilienceContext = new ResilienceContext("timeout-pol", "timeout-op");
        var onTimeoutContext = new TimeoutContext(resilienceContext, timeout: TimeSpan.FromSeconds(5));

        // Act
        Func<Task> act = async () => await options.OnTimeout!(onTimeoutContext);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion
}
