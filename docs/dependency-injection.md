# Dependency Injection & Service Configuration

## Overview

The `EricksonLopez.Resilience.DependencyInjection` package provides clean, idiomatic registration extensions for `Microsoft.Extensions.DependencyInjection`.

## Registered Lifetimes

| Service Contract | Default Lifetime | Implementation Type | Description |
|---|---|---|---|
| `IResiliencePipelineRegistry` | Singleton | `ResiliencePipelineRegistry` | Thread-safe concurrent registry of compiled pipelines. |
| `IResiliencePolicyRegistry` | Singleton | `ResiliencePolicyRegistry` | Registry of defined policy blueprints. |
| `IResultRetryClassifier` | Singleton | `ResultRetryClassifier` | Classifies `Result<T>` and errors for retryability. |
| `IErrorClassifier` | Singleton | `ResultRetryClassifier` | Interface for classifying domain `Error` objects. |
| `IResilienceExecutor` | Singleton | `PollyResilienceExecutor` | High-performance executor delegating to compiled pipelines. |

---

## Registration API

```csharp
using EricksonLopez.Resilience.DependencyInjection;

// 1. Register Core Framework
services.AddEricksonLopezResilience();

// 2. Register Strongly-Typed Policy
services.AddResiliencePolicy<OrderProcessingResiliencePolicy>();

// 3. Register Inline Named Policy
services.AddResiliencePolicy("third-party-api", builder =>
{
    builder
        .AddRetry(opt => opt.MaxRetryAttempts = 3)
        .AddTimeout(TimeSpan.FromSeconds(5));
});
```
