# ADR-006: Prohibition of Synchronous Execution APIs

## Status
Accepted

## Date
2026-09-04

## Context
Legacy resilience frameworks (such as Polly v7) provided blocking synchronous execution methods (`Execute(...)` and `Execute<T>(...)`). In modern high-throughput asynchronous services running on .NET 10, synchronous blocking over resilient pipelines leads to severe architectural risks:
- **Threadpool Starvation**: Blocking worker threads during retries with exponential backoffs or circuit breaker delays exhausts ThreadPool workers.
- **Deadlocks**: Sync-over-async conversions (`.GetAwaiter().GetResult()`) inside asynchronous pipeline continuations risk synchronization context deadlocks.
- **AOT & Runtime Inefficiency**: Violates modern async performance guidelines utilizing `ValueTask`.

## Decision
We explicitly prohibit synchronous blocking execution methods across all public contracts in `EricksonLopez.Resilience`:
1. All methods on `IResilienceExecutor`, `IResiliencePipeline`, and `IResiliencePipeline<TResult>` strictly return `ValueTask` or `ValueTask<TResult>`.
2. No synchronous overloads (`Execute(...)`) will be added to any core or abstraction package.
3. Callers requiring synchronous resilience must invoke asynchronous pipelines from asynchronous call sites using `await`.

## Consequences
### Positive
- Prevents ThreadPool starvation under high concurrency and failure spikes.
- Eliminates sync-over-async deadlock vulnerabilities.
- Maintains high performance and zero-allocation asynchronous paths via `ValueTask`.

### Negative / Tradeoffs
- Consumers maintaining legacy blocking synchronous architectures must refactor code paths to `async`/`await`.
