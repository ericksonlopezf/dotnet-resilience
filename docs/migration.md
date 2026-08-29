# Migration Guide from Legacy Polly

## Overview

This guide describes how to migrate an existing codebase from direct Polly v7 / v8 references to the clean, decoupled `EricksonLopez.Resilience` architectural layer.

## Migration Matrix

| Legacy Polly Pattern | EricksonLopez.Resilience Replacement |
|---|---|
| Direct `IAsyncPolicy` / `AsyncRetryPolicy` | `IResiliencePipeline` / `IResilienceExecutor` |
| Direct `Polly.ResiliencePipeline` in domain services | `IResilienceExecutor.ExecuteAsync("policy-name", ...)` |
| Catching `BrokenCircuitException` directly in application | Catching `CircuitBrokenException` or evaluating `Result<T>` |
| Catching `TimeoutRejectedException` | Catching `ResilienceTimeoutException` |
| Custom retry loops checking HTTP status codes | `ResultRetryClassifier` / `AddResultRetry()` |
| Polly v7 `PolicyWrap` | Sequential builder: `builder.AddRateLimiter().AddTimeout().AddCircuitBreaker().AddRetry()` |

---

## Step-by-Step Migration

### 1. Remove Polly References from Application & Domain Projects
Remove `<PackageReference Include="Polly" />` and `<PackageReference Include="Polly.Core" />` from your domain, application, and use-case projects. Replace with:
```xml
<ProjectReference Include=".../EricksonLopez.Resilience.Abstractions/EricksonLopez.Resilience.Abstractions.csproj" />
```

### 2. Replace Direct Execution Calls
**Before (Coupled to Polly):**
```csharp
// ❌ Application service coupled to Polly
public class OldService
{
    private readonly ResiliencePipeline _pipeline;
    public OldService(ResiliencePipeline pipeline) => _pipeline = pipeline;

    public async Task<string> DoWork() =>
        await _pipeline.ExecuteAsync(async ct => await FetchData(ct));
}
```

**After (Clean Architecture):**
```csharp
// ✅ Application service depends only on first-party abstractions
public class NewService
{
    private readonly IResilienceExecutor _executor;
    public NewService(IResilienceExecutor executor) => _executor = executor;

    public async ValueTask<Result<string>> DoWork(CancellationToken ct) =>
        await _executor.ExecuteAsync("my-policy", async ctx => await FetchData(ctx.CancellationToken), ct);
}
```
