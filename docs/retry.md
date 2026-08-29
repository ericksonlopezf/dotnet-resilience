# Retry Strategy & Jitter

## Overview

The retry strategy handles transient failures by re-executing an operation after a calculated backoff delay.

## Golden Rules of Retries

1. **Never retry non-idempotent operations without an idempotency key.**
2. **Never retry permanent failures** (e.g. `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, domain validation errors).
3. **Always use Jitter** (decorrelated randomized backoff) to avoid the "thundering herd" problem where hundreds of clients retry simultaneously against a recovering service.

---

## Configuration Options

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Options;

builder.AddRetry(options =>
{
    // Maximum number of retry attempts
    options.MaxRetryAttempts = 3;

    // Base backoff delay
    options.Delay = TimeSpan.FromMilliseconds(200);

    // Backoff algorithm: Constant, Linear, Exponential, ExponentialWithJitter
    options.BackoffType = BackoffType.ExponentialWithJitter;

    // Optional custom predicate for exceptions
    options.ShouldHandleException = ex => ex is TimeoutException or IOException;

    // Optional telemetry / lifecycle hook
    options.OnRetry = attemptContext =>
    {
        Console.WriteLine($"Retry attempt #{attemptContext.AttemptNumber} after {attemptContext.Delay.TotalMilliseconds}ms due to {attemptContext.Exception?.Message ?? "Result failure"}");
        return ValueTask.CompletedTask;
    };
});
```

---

## Backoff Types

| Backoff Type | Formula | Ideal Use Case |
|---|---|---|
| `Constant` | $\text{Delay}$ | In-memory operations, local lock acquisitions |
| `Linear` | $\text{Delay} \times \text{Attempt}$ | Low-volume internal services |
| `Exponential` | $\text{Delay} \times 2^{\text{Attempt}-1}$ | Distributed systems with predictable load |
| `ExponentialWithJitter` (Default) | $\text{Random}(0, \text{Delay} \times 2^{\text{Attempt}-1})$ | Public APIs, microservices, high-traffic databases |
