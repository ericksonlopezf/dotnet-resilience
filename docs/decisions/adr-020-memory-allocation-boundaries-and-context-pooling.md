# ADR-020: Memory Allocation Boundaries and Context Pooling Strategy

## Status
Accepted

## Date
2026-09-15

## Context
High-throughput distributed systems in .NET 10 process tens of thousands of requests per second per container. In such environments, garbage collection (GC) pauses directly impact p99 latency SLAs. A key architectural question arose regarding whether the ecosystem-level `ResilienceContext` should be pooled via `ObjectPool<T>` or instantiated per execution request.

Investigation and memory profiling revealed:
1. Reusing a mutable execution context across concurrent threads or requests without complete, guaranteed reset creates severe state contamination vulnerabilities (e.g. leaking `CorrelationId`, `TenantId`, or authorization claims across tenants).
2. The third-party execution engine (Polly v8+) already manages its own internal execution context pool via `global::Polly.ResilienceContextPool.Shared`.
3. The ecosystem's `ResilienceContext` holds only immutable scalars (`PolicyName`, `OperationName`, `CorrelationId`, `TenantId`, `CancellationToken`) and defers property dictionary instantiation via `LazyInitializer`, resulting in zero dictionary allocations on standard fast paths and a bounded overhead of ~32 to 112 bytes.

## Decision
1. **Engine-Level Context Pooling**:
   - `PollyContextAdapter` strictly acquires pooled Polly contexts via `global::Polly.ResilienceContextPool.Shared.Get(...)` and returns them in `finally` blocks via `PollyContextAdapter.Return(...)`.
2. **Ecosystem Context Allocation Boundary**:
   - `ResilienceContext` instances are instantiated on-demand per resilient execution flow (`new ResilienceContext(...)` or `ResilienceContext.Create(...)`).
   - `ResilienceContext` does not participate in global object pooling. This completely eliminates race conditions, multi-tenant property leakage, and synchronization lock contention on thread-local or shared pools.
   - The custom properties dictionary inside `ResilienceContext` remains lazy-initialized on first write, ensuring zero dictionary allocations when custom metadata is not injected.
3. **Documentation Accuracy**:
   - Documentation and benchmarks must accurately describe this model as **Low-Allocation Context Lifecycle** rather than zero-allocation, preserving technical honesty.

## Consequences
### Positive
- Total isolation between concurrent executions and multi-tenant requests.
- Zero state leakage risk across distributed tracing spans and correlation boundaries.
- Full utilization of Polly's high-performance native context pool for the heavy execution state machine.
- Negligible Gen0 allocation pressure with instant, ephemeral garbage collection.

### Negative / Tradeoffs
- Incurs a small, bounded heap allocation (~32 to 112 bytes) per execution invocation.
