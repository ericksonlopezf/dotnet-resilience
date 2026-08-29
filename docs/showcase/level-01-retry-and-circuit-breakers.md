# Level 01: Quick Start & Resilient Execution Pipelines

## 1. Initial Dependency Injection Setup
`EricksonLopez.Resilience` integrates seamlessly with Microsoft Dependency Injection, abstracting underlying execution engines:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 1. Register Core Infrastructure and Polly Engine Adapter
services.AddEricksonLopezResilience();

// 2. Register Named Resilience Policy
services.AddResiliencePolicy("quickstart-policy", builder =>
{
    builder
        .AddTimeout(TimeSpan.FromSeconds(2))
        .AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(100);
            opt.BackoffType = BackoffType.ExponentialWithJitter;
        })
        .AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 5;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromSeconds(5);
        });
});

var serviceProvider = services.BuildServiceProvider();
var executor = serviceProvider.GetRequiredService<IResilienceExecutor>();
```

---

## 2. Executing Operations via `IResilienceExecutor`

### Returning a Typed Value
```csharp
var result = await executor.ExecuteAsync(
    "quickstart-policy",
    async (ResilienceContext context) =>
    {
        // Executes protected async work with contextual tracing metadata
        return await httpClient.GetStringAsync("https://api.example.com/data", context.CancellationToken);
    },
    ResilienceContext.Create("quickstart-policy", cancellationToken),
    cancellationToken);
```

### Executing Void Actions
```csharp
await executor.ExecuteAsync(
    "quickstart-policy",
    async (CancellationToken ct) =>
    {
        await messagePublisher.PublishAsync(eventMessage, ct);
    },
    cancellationToken);
```
