# ADR-008: Automatic Telemetry and Structured Logging Integration

## Status
Accepted

## Date
2026-09-04

## Context
Resilience mechanisms must be observable in production systems to detect cascading failures, degraded dependencies, and throttling. In previous versions, consumers had to manually invoke `ResilienceMeter` or attach logging callbacks. Modern cloud-native microservices expect out-of-the-box OpenTelemetry metrics and structured logging whenever a resilient execution occurs.

## Decision
1. **Automatic OpenTelemetry Instrumentation**:
   - `PollyResilienceExecutor` automatically records metrics via `ResilienceMeter` (`resilience.execution.duration`, `resilience.retry.attempts`, `resilience.circuit_breaker.state_changes`, `resilience.timeout.rejections`, `resilience.rate_limiter.rejections`) on every execution, incorporating `policyName`, `operationName`, and `tenantId` tags.
   - `ResilienceActivitySource` generates distributed tracing spans around resilient operations with status and exception tags.
2. **Optional Structured Logging (`ILogger`)**:
   - `PollyResilienceExecutor` accepts an optional `ILogger<PollyResilienceExecutor>` injected via Dependency Injection.
   - Resilience events (retry attempts, circuit breaker state changes, timeouts, rate limit rejections) are logged at appropriate log levels (Debug, Warning, Information) with structured log parameters.
3. **Opt-Out & Zero-Overhead Mode**:
   - If `ILogger` is not registered or if metrics recording is disabled, the execution path incurs minimal overhead with zero extra allocations.

## Consequences
### Positive
- Instant visibility into resilience health across Prometheus, Grafana, OpenTelemetry collectors, and cloud monitors without consumer boilerplate.
- Multi-tenant observability enabled by default via `tenantId` tagging.
- Transparent structured logging for diagnostics and incident response.

### Negative / Tradeoffs
- Slight metric recording overhead (sub-microsecond histogram timer) on hot paths.
