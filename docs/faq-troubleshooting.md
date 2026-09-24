# FAQ & Troubleshooting: EricksonLopez.Resilience

Frequently asked questions, common pitfalls, diagnostic guides, and performance recommendations for `EricksonLopez.Resilience`.

---

## Frequently Asked Questions (FAQ)

### 1. Why does `EricksonLopez.Resilience` encapsulate Polly v8 rather than using it directly?
**Answer**: Clean Architecture and Domain-Driven Design require domain and application layers to remain independent of third-party infrastructure libraries. By wrapping Polly behind first-party interfaces (`IResilienceExecutor`, `IResiliencePipeline`, `ResilienceContext`), application code avoids breaking changes across major library versions, enables seamless unit testing via `PassthroughResiliencePipeline`, and automatically integrates domain `Result<T>` classifications and OpenTelemetry.

### 2. Can I retry database operations with entity tracking?
**Answer**: Yes, but you must open and commit the database transaction / Unit of Work *inside* the delegate passed to `ExecuteAsync`. Never wrap the `ExecuteAsync` invocation inside a `using var transaction` block, as retrying on a failed, dirty DbContext results in invalid state tracking.

### 3. Does `EricksonLopez.Resilience` support Native AOT and Trimming?
**Answer**: Yes, 100%. All assemblies are compiled with `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. Configuration binding uses reflection-free static parsing rather than reflection.

---

## Troubleshooting Guide

| Symptom | Probable Cause | Recommended Resolution |
|---|---|---|
| `ResiliencePolicyNotFoundException` | Policy was not registered during `IServiceCollection` startup. | Call `services.AddResiliencePolicy(name, ...)` or verify the policy name string. |
| Non-retryable validation errors triggering retries | Default exception handler or custom predicate treats all errors as transient. | Use `AddResultRetry()` which evaluates `Error.Retryability` and `Error.Type` automatically. |
| Circuit breaker opens too frequently | `MinimumThroughput` is set too low for normal traffic bursts. | Increase `MinimumThroughput` (e.g. 20+) and adjust `SamplingDuration` to at least 30 seconds. |
| Rate limiter throwing `RateLimitRejectedException` immediately | `QueueLimit` is set to 0. | If buffering is desired, set `QueueLimit > 0` on `RateLimiterStrategyOptions`. |
| Excessive allocations in high-throughput benchmarks | Populating custom properties dictionary when not required or allocating heavy closures. | Use `ResilienceContext.Create(policyName)` which maintains an unallocated properties bag until first write; underlying Polly execution contexts are pooled automatically via `PollyContextAdapter`. |
