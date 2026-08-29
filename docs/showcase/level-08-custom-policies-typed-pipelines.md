# Level 08: Customization — Typed Policies & Generic Pipelines

## 1. Strongly-Typed Reusable Policies
Encapsulate complex strategy chains into reusable, strongly-typed policy definitions deriving from `ResiliencePolicy`:

```csharp
using System;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Policies;
using Microsoft.Extensions.DependencyInjection;

public sealed class EnterprisePaymentResiliencePolicy : ResiliencePolicy
{
    public const string PolicyName = "EnterprisePaymentPolicy";

    public override string Name => PolicyName;

    public override void Configure(IResiliencePipelineBuilder builder)
    {
        builder
            .AddTimeout(TimeSpan.FromSeconds(5))
            .AddRetry(opt =>
            {
                opt.MaxRetryAttempts = 3;
                opt.Delay = TimeSpan.FromMilliseconds(100);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
            })
            .AddCircuitBreaker(opt =>
            {
                opt.FailureRatio = 0.5;
                opt.MinimumThroughput = 10;
                opt.SamplingDuration = TimeSpan.FromSeconds(30);
                opt.BreakDuration = TimeSpan.FromSeconds(15);
            });
    }
}

// DI Registration:
services.AddResiliencePolicy<EnterprisePaymentResiliencePolicy>();
```

---

## 2. Strongly-Typed Pipelines with Fallback & Hedging
For operations that return a specific `TResult`, use `ResiliencePipelineBuilder<TResult>` with fallback and speculative parallel hedging:

```csharp
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Registration;

PollyResilienceRegistration.RegisterTypedPipeline<string>();

var typedPipeline = new ResiliencePipelineBuilder<string>("CatalogSearchPipeline")
    .AddTimeout(TimeSpan.FromSeconds(2))
    .AddFallback(opt =>
    {
        opt.FallbackAction = ctx => ValueTask.FromResult("Cached Catalog Results [Offline Mode]");
    })
    .Build();

var result = await typedPipeline.ExecuteAsync(async ct =>
{
    throw new TimeoutException("Database read timeout");
}, cancellationToken);

// Returns: "Cached Catalog Results [Offline Mode]"
```

---

## 3. Unit Testing with `PassthroughResiliencePipeline`
Execute unit tests without strategy overhead or background timers using `PassthroughResiliencePipeline`:

```csharp
IResiliencePipeline testPipeline = new PassthroughResiliencePipeline("UnitTest");
var result = await testPipeline.ExecuteAsync(async ct => await service.ComputeAsync(ct));
```
