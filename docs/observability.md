# Observability & OpenTelemetry

## Overview

`EricksonLopez.Resilience.OpenTelemetry` provides zero-overhead, production-ready telemetry instrumentation using standard .NET `Meter` and `ActivitySource` APIs.

## Metrics Catalog

Meter Name: `EricksonLopez.Resilience`

| Metric Name | Instrument Type | Unit | Description | Semantic Tags |
|---|---|---|---|---|
| `resilience.execution.duration` | Histogram | `ms` | Total elapsed duration of resilient executions. | `resilience.policy`, `resilience.operation`, `resilience.status` |
| `resilience.retry.attempts` | Counter | `{attempt}` | Number of retry attempts executed. | `resilience.policy`, `resilience.retry_number` |
| `resilience.circuit_breaker.state_changes` | Counter | `{transition}` | Count of circuit breaker state transitions. | `resilience.policy`, `resilience.circuit_state` |
| `resilience.timeout.rejections` | Counter | `{rejection}` | Count of operations cancelled by timeout limit. | `resilience.policy` |
| `resilience.rate_limiter.rejections` | Counter | `{rejection}` | Count of operations rejected by rate limits. | `resilience.policy` |

---

## Distributed Tracing Spans

ActivitySource Name: `EricksonLopez.Resilience`

Spans are created with name format `Resilience.Execute:{PolicyName}` containing:
- `resilience.policy`: Policy identifier
- `resilience.operation`: Logical operation name
- `resilience.attempt`: Current attempt number
- `resilience.correlation_id`: Distributed correlation ID
- `resilience.tenant_id`: Multi-tenant identifier

---

## OpenTelemetry Collector Setup

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("EricksonLopez.Resilience");
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource("EricksonLopez.Resilience");
    });
```
