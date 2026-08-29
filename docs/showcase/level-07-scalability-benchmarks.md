# Level 07: Scalability — Zero-Allocation Pooling & High Throughput

## 1. Zero-Allocation Context Pooling
`EricksonLopez.Resilience` achieves ultra-high throughput by leveraging Polly v8 zero-allocation pipeline delegates and internal object pooling:

- **ValueTask Everywhere**: All pipeline execution paths return `ValueTask` / `ValueTask<TResult>` to avoid task heap allocations on synchronous and cached completions.
- **Context Pooling**: Context instances are recycled via `ResilienceContextPool`, ensuring high-traffic web APIs and message consumers do not trigger garbage collection pressure.

---

## 2. High-Throughput Execution Benchmark Pattern

```csharp
using System.Diagnostics;
using System.Threading.Tasks;
using EricksonLopez.Resilience;

var context = ResilienceContext.Create("high-throughput-policy");
var sw = Stopwatch.StartNew();

const int iterations = 100_000;
for (int i = 0; i < iterations; i++)
{
    await executor.ExecuteAsync(
        "high-throughput-policy",
        async ctx => await ValueTask.FromResult(1),
        context);
}

sw.Stop();
var opsPerSec = iterations / sw.Elapsed.TotalSeconds;
Console.WriteLine($"Throughput: {opsPerSec:N0} operations/second");
```

---

## 3. Comparative Allocation Table

| Strategy Layer | Allocations (Success Path) | Memory Overhead |
|---|---|---|
| **Passthrough Pipeline** | 0 Bytes | None |
| **Timeout Check** | 0 Bytes | Struct CancellationTokenSource |
| **Circuit Breaker (Closed)** | 0 Bytes | Atomic read |
| **Rate Limiter (Admitted)** | 0 Bytes | Lock-free lease |
| **Retry Strategy (No failure)** | 0 Bytes | Struct delegate execution |
