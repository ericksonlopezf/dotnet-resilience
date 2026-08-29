# Level 02: Full Configuration, Options & Presets

## 1. Exhaustive Resilience Strategy Configuration
Configure rate limiting, timeouts, exponential jitter retries, circuit breakers, and hedging with custom lifecycle callbacks:

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEricksonLopezResilience();

services.AddResiliencePolicy("exhaustive-policy", builder =>
{
    builder
        // 1. Rate Limiting (Sliding Window, Fixed Window, Token Bucket, or Concurrency)
        .AddRateLimiter(options =>
        {
            options.Name = "GlobalIngressLimiter";
            options.PermitLimit = 100;
            options.QueueLimit = 10;
            options.Window = TimeSpan.FromSeconds(30);
            options.LimiterType = RateLimiterType.SlidingWindow;
            options.OnRejected = context =>
            {
                Console.WriteLine($"Rejected quota. Retry-After: {context.RetryAfter?.TotalSeconds}s");
                return ValueTask.CompletedTask;
            };
        })
        // 2. Timeout
        .AddTimeout(options =>
        {
            options.Timeout = TimeSpan.FromSeconds(5);
            options.OnTimeout = context =>
            {
                Console.WriteLine($"Timed out on: {context.ResilienceContext.OperationName}");
                return ValueTask.CompletedTask;
            };
        })
        // 3. Retry with Exponential Jitter
        .AddRetry(options =>
        {
            options.MaxRetryAttempts = 3;
            options.Delay = TimeSpan.FromMilliseconds(150);
            options.MaxDelay = TimeSpan.FromSeconds(2);
            options.BackoffType = BackoffType.ExponentialWithJitter;
            options.OnRetry = context =>
            {
                Console.WriteLine($"Retry #{context.AttemptNumber} after {context.Delay.TotalMilliseconds}ms");
                return ValueTask.CompletedTask;
            };
        })
        // 4. Circuit Breaker
        .AddCircuitBreaker(options =>
        {
            options.FailureRatio = 0.5;
            options.MinimumThroughput = 10;
            options.SamplingDuration = TimeSpan.FromSeconds(30);
            options.BreakDuration = TimeSpan.FromSeconds(15);
            options.OnCircuitOpened = ctx => ValueTask.CompletedTask;
            options.OnCircuitHalfOpened = ctx => ValueTask.CompletedTask;
            options.OnCircuitClosed = ctx => ValueTask.CompletedTask;
        });
});
```

---

## 2. Standard & Specialized Architectural Presets

```csharp
using EricksonLopez.Resilience.Extensions;

// Standard Preset: 30s Timeout + 3 Retries (Exponential Jitter) + Circuit Breaker
services.AddResiliencePolicy("standard-preset", builder => builder.AddStandardResilience());

// Database Preset: Optimized for SQL deadlocks and transient connection drops
services.AddResiliencePolicy("database-preset", builder => 
    builder.AddDatabaseResilience(timeout: TimeSpan.FromSeconds(10), maxRetries: 4));
```

---

## 3. Dynamic Configuration via `IConfigurationSection`

```csharp
// Zero-reflection, Native AOT-safe configuration binding
var policySection = configuration.GetSection("Resilience:PaymentPolicy");
services.AddResiliencePolicyFromConfiguration("config-bound-policy", policySection);
```
