# ADR-012: Rejection of Hystrix-style Dashboard UI

## Status
Rejected

## Date
2026-09-04

## Context
Some resilience libraries (notably Netflix Hystrix and early Steeltoe implementations) provide a dashboard UI for real-time monitoring of circuit breaker states, retry rates, and throughput. This was evaluated as a potential feature to aid operational visibility.

## Decision
**No dashboard or health-monitoring UI will be included in `EricksonLopez.Resilience`.**

Rationale:
1. **Wrong Abstraction Layer**: A dashboard UI is a visualization and infrastructure concern, not a resilience mechanism. Embedding it in a library conflates the library's responsibility and dramatically increases its surface area.
2. **OpenTelemetry Supersedes It**: All resilience events emit structured OTel metrics (`ResilienceMeter`) and distributed tracing spans (`ResilienceActivitySource`). Any OTel-compatible backend (Prometheus + Grafana, Azure Monitor, Datadog, Jaeger) provides a richer dashboard than any bundled UI.
3. **Maintenance Cost**: A proprietary dashboard UI requires frontend development, security review, web infrastructure, and long-term maintenance — disproportionate to the value it adds when industry-standard observability tooling already exists.
4. **Multi-Tenant Metrics Already Available**: The `tenantId` tag on all metrics allows building per-tenant dashboards in any OTel backend without custom UI code.

## Consequences
### Positive
- Library footprint remains minimal (no frontend assets, no HTTP endpoints).
- Observability is achieved through industry-standard OTel exporters.

### Negative / Tradeoffs
- Users must set up an OTel exporter and dashboard tool (Prometheus, Grafana, etc.) to visualize metrics. This is an expected prerequisite for cloud-native applications.
