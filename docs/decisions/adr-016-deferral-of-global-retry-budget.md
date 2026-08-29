# ADR-016: Deferral of Global Retry Budget

## Status
**Deferred** — Advanced feature for high-scale scenarios; revisit at v2.x

## Context
A "retry budget" is a system-wide mechanism that caps the total number of retry attempts across all concurrent operations within a time window. This prevents retry storms under cascading failure conditions where many concurrent operations simultaneously trigger retries, amplifying load on an already-stressed downstream service. The feature was considered for inclusion during the competitive analysis phase.

## Decision
**A global retry budget mechanism is explicitly deferred from the current roadmap.** Individual pipeline retry limits (`RetryStrategyOptions.MaxRetryAttempts`) remain the supported retry governance mechanism.

Rationale:
1. **Niche Use Case**: Retry storms are a concern for systems processing thousands of concurrent requests where individual per-pipeline limits are insufficient. The primary target segment (DDD applications with moderate load) is unlikely to encounter this problem before v1.x maturity.
2. **Distributed State Complexity**: A meaningful retry budget requires shared mutable state across concurrent pipelines. In distributed/multi-instance deployments, this requires Redis or similar coordination — well beyond the scope of an in-process resilience library.
3. **Prerequisite Missing**: A per-process in-memory retry budget requires an atomic counter and time-windowed tracking that introduces contention on high-throughput paths. Designing this correctly for Native AOT and zero-allocation environments requires significant engineering investment.
4. **Rate Limiter Approximation**: The existing `RateLimiterStrategyOptions` (with `SlidingWindow` or `FixedWindow`) can approximate retry budget behavior by rate-limiting the executor entry point, providing an acceptable substitute for most use cases.

## Revisit Criteria
- Documented evidence of retry storm incidents in production from users.
- `IResiliencePipeline<TResult>` ecosystem reaches sufficient maturity (v2.0+).
- A distributed-state-free in-process budget design is validated.
