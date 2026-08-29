# Hedging Strategy

## Overview

Hedging (speculative execution) executes secondary attempts of an operation when the primary execution does not complete within an expected latency threshold, returning whichever attempt finishes successfully first.

## Constraints & Architectural Rules

- **Hedging is strictly forbidden for non-idempotent operations.** Never hedge state-mutating commands, database writes, or payment transactions unless guaranteed by distributed idempotency keys.
- Hedging is ideal for read replicas, DNS lookups, distributed caches, and redundant query endpoints to eliminate P95/P99 tail latency spikes.

---

## Typed vs Untyped Hedging (ADR-010)

`EricksonLopez.Resilience` explicitly separates untyped pipelines from typed pipelines:

### 1. Strongly-Typed Parallel Hedging (`IResiliencePipeline<TResult>`)

In typed pipelines, hedging launches **true concurrent parallel executions** if the primary attempt does not complete within `Delay`. It supports custom alternative action generators (e.g. directing the secondary request to a read replica).

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;

var builder = new ResiliencePipelineBuilder<CustomerDto>("customer-query-hedging");

builder.AddHedging(options =>
{
    // Maximum number of parallel hedged attempts (e.g. 2 additional parallel requests)
    options.MaxHedgedAttempts = 2;

    // Latency threshold before launching secondary speculative request
    options.Delay = TimeSpan.FromMilliseconds(150);

    // Lifecycle notification callback
    options.OnHedging = ctx =>
    {
        Console.WriteLine($"Launching parallel hedge attempt #{ctx.AttemptNumber} for policy '{ctx.ResilienceContext?.PolicyName}'");
        return ValueTask.CompletedTask;
    };

    // Optional: Route hedged attempt to secondary read-replica endpoint
    options.HedgedActionGenerator = ctx =>
    {
        return async () => await queryClient.FetchFromReplicaAsync(customerId, ctx.ResilienceContext?.CancellationToken ?? default);
    };
});
```

---

### 2. Untyped Sequential Hedging (`IResiliencePipeline`)

In untyped void pipelines, hedging operates as a **sequential speculative retry** — re-executing the same delegate after `Delay` without launching overlapping threads.

```csharp
using System;
using EricksonLopez.Resilience.Options;

builder.AddHedging(options =>
{
    options.MaxHedgedAttempts = 2;
    options.Delay = TimeSpan.FromMilliseconds(200);
});
```

---

## Strategy Options Reference

| Property | Type | Default | Description |
|---|---|---|---|
| `MaxHedgedAttempts` | `int` | `2` | Maximum number of additional hedged attempts. |
| `Delay` | `TimeSpan` | `500ms` | Duration to wait before spawning the next attempt. |
| `ShouldHandleException` | `Func<Exception, bool>?` | `null` | Predicate triggering immediate hedge without waiting for delay. |
| `ShouldHandleResult` | `Func<TResult, bool>?` | `null` | Predicate on outcome result triggering immediate hedge. |
| `OnHedging` *(Typed only)* | `Func<HedgingContext, ValueTask>?` | `null` | Asynchronous callback invoked when a hedged attempt begins. |
| `HedgedActionGenerator` *(Typed only)* | `Func<HedgingContext, Func<ValueTask<TResult>>?>?` | `null` | Generator providing alternate speculative delegates (e.g. read replicas). |
