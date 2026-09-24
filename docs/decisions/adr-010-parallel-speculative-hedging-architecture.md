# ADR-010: Parallel Speculative Hedging Strategy Architecture

## Status
Accepted

## Date
2026-09-04

## Context
In high-throughput, low-latency microservices, tail latency (p95/p99) is frequently degraded by transient network congestion or slow replica nodes. **Hedging** addresses this by launching parallel speculative attempts if the primary execution does not complete within a specified delay threshold, returning the result of whichever attempt finishes first.

In previous revisions, non-generic pipelines simulated hedging through sequential retries, which did not reduce tail latency and created a false sense of parallelism.

## Decision
1. **Parallel Hedging in Typed Pipelines (`IResiliencePipeline<TResult>`)**:
   - Real parallel speculative hedging is supported exclusively in typed pipelines via `HedgingStrategyOptions<TResult>` and `AddHedging<TResult>(...)`.
2. **Polly Engine Mapping**:
   - `PollyPipelineBuilderTranslator` maps `HedgingStrategyOptions<TResult>` to `global::Polly.Hedging.HedgingStrategyOptions<TResult>`, executing concurrent tasks in parallel and canceling slower redundant tasks when the earliest attempt completes successfully.
3. **Idempotency and Side-Effect-Free Invariant**:
   - Parallel hedging is strictly constrained to read-only queries and idempotent operations. The documentation and architecture explicitly warn against configuring hedging for mutating commands or stateful transactions.

## Consequences
### Positive
- Genuine reduction in tail latency (p95/p99) for read-intensive distributed workloads.
- Eliminates false parity in hedging implementations.
- True cancellation of redundant speculative executions when the primary or fastest attempt finishes.

### Negative / Tradeoffs
- Increases load on downstream read replicas due to speculative concurrent requests.
- Requires callers to ensure target endpoints are side-effect-free and idempotent.
