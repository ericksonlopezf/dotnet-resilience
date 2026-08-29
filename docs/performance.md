# Performance & Allocation Benchmarks

## Overview

High-throughput systems require that the resilience abstraction adds virtually zero CPU overhead and minimal heap allocations.

`EricksonLopez.Resilience` achieves this by:
1. Reusing Polly v8's `ResilienceContextPool`.
2. Struct-based `ValueTask` return paths for synchronous / cached completions.
3. Lock-free `ConcurrentDictionary` lookups in `ResiliencePipelineRegistry`.
4. Zero reflection during runtime execution.

---

## Allocation Profile

| Execution Pattern | Heap Allocations (Success Path) | Overhead vs Direct Polly |
|---|---|---|
| Direct Polly v8 Execution | ~0 - 48 bytes | Baseline |
| `IResiliencePipeline.ExecuteAsync` | ~0 - 64 bytes | < 16 bytes (Context wrapper) |
| `IResilienceExecutor.ExecuteAsync` (with Registry lookup) | ~0 - 64 bytes | < 20 ns |
| `ResiliencePipelineBehavior` (Mediator) | ~DI class overhead; struct-continuation minimizes closure allocs | < 10 ns |

---

## Running Benchmarks

To execute the benchmark suite:

```bash
dotnet run -c Release --project benchmarks/EricksonLopez.Resilience.Benchmarks
```
