# Level 03: Real-World Use Cases & Result Pattern

## 1. Domain Error vs Transient Failure Classification
In enterprise architectures, operations return `Result<T>` instead of throwing exceptions for expected business failures. `EricksonLopez.Resilience` deterministically distinguishes transient infrastructure failures from permanent domain/validation errors:

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEricksonLopezResilience();

services.AddResiliencePolicy("payment-gateway", builder =>
{
    builder
        .AddTimeout(TimeSpan.FromSeconds(3))
        .AddResultRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(100);
            opt.BackoffType = BackoffType.ExponentialWithJitter;
            opt.OnRetry = ctx =>
            {
                Console.WriteLine($"Retrying attempt #{ctx.AttemptNumber} due to transient outcome...");
                return ValueTask.CompletedTask;
            };
        });
});

var sp = services.BuildServiceProvider();
var executor = sp.GetRequiredService<IResilienceExecutor>();
```

---

## 2. Automatic Retry on `Error.Unavailable` / `Error.Infrastructure`
When an operation returns `Result.Failure(Error.Unavailable(...))` or `Result.Failure(Error.Infrastructure(...))` or an error with `ErrorRetryability.Transient`, the policy retries automatically:

```csharp
var result = await executor.ExecuteAsync(
    "payment-gateway",
    async (ResilienceContext ctx) =>
    {
        var response = await paymentGatewayClient.ChargeCardAsync(amount, ctx.CancellationToken);
        if (response.IsBusy)
        {
            // RETRIED AUTOMATICALLY
            return Result<PaymentReceipt>.Failure(
                Error.Unavailable("Gateway.Busy", "Payment processor temporarily unavailable"));
        }

        return Result<PaymentReceipt>.Success(response.Receipt);
    },
    ResilienceContext.Create("payment-gateway"));
```

---

## 3. Fast-Fail on Non-Retryable Domain & Validation Errors
Validation failures (`Error.Validation`), business rule violations (`Error.Domain`), or permanent errors (`ErrorRetryability.Permanent`) exit immediately without wasting retry budgets:

```csharp
var domainErrorResult = await executor.ExecuteAsync(
    "payment-gateway",
    async (ResilienceContext ctx) =>
    {
        // NOT RETRIED: Exits on attempt 1 immediately
        return Result<PaymentReceipt>.Failure(
            Error.Validation("Card.Expired", "The credit card expiration date is invalid"));
    },
    ResilienceContext.Create("payment-gateway"));
```
