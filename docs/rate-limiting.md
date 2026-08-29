# Rate Limiting & Concurrency Control

## Overview

The rate limiter strategy controls the rate and concurrency of executions to prevent overloading protected resources (e.g. external payment APIs, database pools, internal CPUs).

## Configuration

`EricksonLopez.Resilience` integrates with `System.Threading.RateLimiting` and `Polly.RateLimiting` under the hood:

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Options;

builder.AddRateLimiter(options =>
{
    // Maximum executions allowed in window
    options.PermitLimit = 100;

    // Maximum requests queued if permit limit reached
    options.QueueLimit = 10;

    // Replenishment time window duration
    options.Window = TimeSpan.FromMinutes(1);

    options.OnRejected = ctx =>
    {
        Console.WriteLine($"Rate limit exceeded for policy '{ctx.ResilienceContext?.PolicyName}'. Retry after: {ctx.RetryAfter}");
        return ValueTask.CompletedTask;
    };
});
```

---

## Exception Handling

When the rate limit or queue capacity is exhausted, the execution throws `EricksonLopez.Resilience.Exceptions.RateLimitRejectedException`:

```csharp
try
{
    await executor.ExecuteAsync("billing-api-policy", async ctx => await ProcessAsync(ctx.CancellationToken));
}
catch (RateLimitRejectedException ex)
{
    Console.WriteLine($"Rejected due to rate limiting on policy '{ex.PolicyName}'. Retry after: {ex.RetryAfter}");
}
```
