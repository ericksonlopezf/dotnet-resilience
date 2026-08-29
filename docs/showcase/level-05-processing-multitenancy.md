# Level 05: Processing — Concurrency, Background Workers & Multi-Tenancy

## 1. Multi-Tenant Execution Context
`ResilienceContext` provides zero-allocation context propagation carrying tenant identity, correlation identifiers, and custom properties across execution attempts:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience;

var context = new ResilienceContext(
    policyName: "background-worker-policy",
    operationName: "ProcessBatchInvoiceJob",
    correlationId: "CORR-883311",
    tenantId: "TENANT-ACME")
    .SetProperty("BatchSize", 500)
    .SetProperty("QueueName", "invoices.incoming");

if (context.TryGetProperty<int>("BatchSize", out var size))
{
    Console.WriteLine($"Processing batch size {size} for Tenant '{context.TenantId}'");
}

await executor.ExecuteAsync(
    "background-worker-policy",
    async (ResilienceContext ctx) =>
    {
        await invoiceProcessor.ProcessAsync(ctx.TenantId, ctx.CancellationToken);
    },
    context);
```

---

## 2. Cooperative Cancellation Propagation
When long-running background tasks receive cancellation tokens (e.g. during application shutdown), `EricksonLopez.Resilience` propagates the cancellation immediately without triggering unnecessary retry attempts:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

try
{
    await executor.ExecuteAsync(
        "background-worker-policy",
        async (CancellationToken ct) =>
        {
            while (!ct.IsCancellationRequested)
            {
                await worker.PollNextMessageAsync(ct);
            }
        },
        cts.Token);
}
catch (OperationCanceledException)
{
    // Clean, expected shutdown path
    logger.LogInformation("Background worker stopped gracefully.");
}
```

---

## 3. Concurrency Limits & Resource Protection
Prevent starvation of shared resources with strict concurrency and sliding-window rate limiters:

```csharp
builder.AddRateLimiter(opt =>
{
    opt.PermitLimit = 10;
    opt.QueueLimit = 2;
    opt.LimiterType = RateLimiterType.Concurrency;
    opt.OnRejected = ctx =>
    {
        logger.LogWarning("Concurrency limit reached for tenant {TenantId}", ctx.ResilienceContext.TenantId);
        return ValueTask.CompletedTask;
    };
});
```
