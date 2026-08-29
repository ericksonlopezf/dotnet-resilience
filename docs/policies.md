# Resilience Policies

## Overview

A resilience policy defines a named, reusable configuration of strategies (Retry, Circuit Breaker, Timeout, Rate Limiting, Hedging) tailored for a specific external dependency or internal workload.

## Defining Policies

There are two primary ways to declare policies in `EricksonLopez.Resilience`:
1. **Strongly-Typed Classes** (`ResiliencePolicy` base class): Preferred for enterprise applications with explicit architecture rules.
2. **Inline Configuration Action Delegates**: Useful for quick prototypes and simple services.

---

### Strongly-Typed Policy Class

Inherit from `ResiliencePolicy` and implement `Name` and `Configure`:

```csharp
using System;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Policies;

public sealed class DatabaseResiliencePolicy : ResiliencePolicy
{
    public override string Name => "sql-database-policy";

    public override void Configure(IResiliencePipelineBuilder builder)
    {
        builder
            .AddRetry(opt =>
            {
                opt.MaxRetryAttempts = 3;
                opt.Delay = TimeSpan.FromMilliseconds(50);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
            })
            .AddTimeout(TimeSpan.FromSeconds(10));
    }
}
```

Registration in DI:
```csharp
services.AddResiliencePolicy<DatabaseResiliencePolicy>();
```

---

### Standard Ecosystem Defaults

The `ResiliencePipelineBuilderExtensions` class provides pre-configured profiles:

```csharp
// Standard profile (Timeout 30s + Retry 3 attempts exponential jitter + Circuit Breaker)
builder.AddStandardResilience();

// Database profile (Timeout 15s + ResultRetry with short exponential jitter backoff)
builder.AddDatabaseResilience(TimeSpan.FromSeconds(15), maxRetries: 3);

// Result-aware retry profile
builder.AddResultRetry(opt =>
{
    opt.MaxRetryAttempts = 3;
    opt.Delay = TimeSpan.FromMilliseconds(100);
    opt.BackoffType = BackoffType.ExponentialWithJitter;
});
```
