# ADR-011: Rejection of Caching as a Resilience Strategy

## Status
Rejected

## Date
2026-09-04

## Context
During product strategy analysis, caching was considered as a potential resilience strategy — specifically, a "CacheStrategy" or "StaleWhileRevalidate" that would return a cached value on failure instead of propagating the error. This pattern is sometimes conflated with graceful degradation via fallback.

## Decision
**Caching is explicitly excluded from `EricksonLopez.Resilience` as a first-class strategy.**

Rationale:
1. **SRP Violation**: Caching is an optimization of latency and resource utilization, not a resilience mechanism. Combining it with retry, circuit breaker, and timeout strategies violates the Single Responsibility Principle at the library level.
2. **Dependency Coupling**: Implementing a caching strategy requires coupling to `IMemoryCache`, `IDistributedCache`, or `IFusionCache` — dependencies that belong in the application infrastructure, not in a resilience library.
3. **Fallback Supersedes It**: The already-implemented `FallbackStrategyOptions<TResult>` (ADR-007) can compose with application-level caching inside the `FallbackAction` delegate. This provides the same capability without coupling.
4. **Consumer Delegation Pattern**: Any user requiring "return cached value on failure" can implement it in the delegate passed to `IResilienceExecutor.ExecuteAsync`:
   ```csharp
   executor.ExecuteAsync("my-policy", async ctx =>
   {
       try { return await repo.GetAsync(id); }
       catch { return cache.Get(id); } // cache-aside in the delegate
   }, context);
   ```

## Consequences
### Positive
- Library remains focused on fault-tolerance, not caching concerns.
- No dependency on `IMemoryCache` or distributed cache packages.
- `FallbackStrategyOptions<TResult>` already satisfies cache-aside fallback patterns.

### Negative / Tradeoffs
- Developers expecting an integrated "cache on failure" strategy must compose it externally.
