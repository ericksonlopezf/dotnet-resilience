# ADR-003: Zero-Allocation Context Lifecycles and Struct-Based Pipeline Behaviors

## Status
**Accepted**

## Context
High-throughput microservices process thousands of requests per second. Allocating reference objects (such as contexts, continuations, and closures) on every resilient execution creates severe Garbage Collection (GC) pressure and latency spikes.

## Decision
1. **Polly Context Object Pooling**:
   - `PollyContextAdapter` utilizes thread-safe object pooling (`PollyContextAdapter.ToPollyContext` and `PollyContextAdapter.Return`) to reuse underlying `Polly.ResilienceContext` instances across execution pipelines without per-request allocations for the execution engine.
2. **Immutable Ecosystem `ResilienceContext`**:
   - `EricksonLopez.Resilience.ResilienceContext` maintains an immutable, thread-safe model carrying `PolicyName`, `OperationName`, `CorrelationId`, and `TenantId`. In high-frequency hot paths, execution wrappers avoid extraneous property allocations.
3. **Struct-Based Continuations**:
   - In `EricksonLopez.Resilience.Mediator`, `ResiliencePipelineBehavior<TRequest, TResponse>` consumes continuation delegates as generic struct constraints (`where TNext : struct, INext<TResponse>`). This minimizes continuation allocations on the hot path.
4. **ValueTask Operations**:
   - All execution signatures in `IResilienceExecutor`, `IResiliencePipeline`, `IResiliencePipeline<TResult>`, and `IPipelineBehavior` return `ValueTask` or `ValueTask<TResult>` to eliminate async state machine allocations on synchronous completions.

## Consequences
### Positive
- Minimized heap allocations on synchronous and cached execution paths (struct continuations, ValueTask, Polly context pooling).
- Low GC gen0/gen1 pressure under sustained high-throughput workloads.
- Polly execution context instances are efficiently pooled and recycled.
- Fully compatible with .NET 10 high-performance runtime optimizations.


### Negative / Tradeoffs
- Requires disciplined returning of underlying Polly contexts in `finally` blocks within adapters.
