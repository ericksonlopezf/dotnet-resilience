// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Level 5 — Processing: Background Processing, Quota/Concurrency Limits, Cancellation, and Multi-Tenant Isolation.
/// </summary>
public static class Level5Processing
{
    private const string WorkerPolicyName = "background-worker-policy";

    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 5] PROCESSING: CONCURRENCY, BACKGROUND WORKERS AND MULTI-TENANCY");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // Policy for background workers with rate limiting and retries
        services.AddResiliencePolicy(WorkerPolicyName, builder =>
        {
            builder
                .AddRateLimiter(opt =>
                {
                    opt.PermitLimit = 5;
                    opt.QueueLimit = 0;
                    opt.Window = TimeSpan.FromSeconds(1);
                    opt.OnRejected = ctx =>
                    {
                        Console.WriteLine($"    [WorkerRateLimiter] Quota exceeded for Tenant='{ctx.ResilienceContext.TenantId}'.");
                        return ValueTask.CompletedTask;
                    };
                })
                .AddTimeout(TimeSpan.FromSeconds(1))
                .AddRetry(opt =>
                {
                    opt.MaxRetryAttempts = 2;
                    opt.Delay = TimeSpan.FromMilliseconds(50);
                });
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        // 1. Multi-Tenant isolation demonstration with enriched custom properties
        Console.WriteLine("\n --- 1. Execution with Enriched Context (Tenant & Correlation) ---");
        var context = new ResilienceContext(WorkerPolicyName, "ProcessInvoiceJob", correlationId: "CORR-883311", tenantId: "TENANT-ACME")
            .SetProperty("InvoiceBatchId", 9942)
            .SetProperty("SourceQueue", "invoices.incoming");

        if (context.TryGetProperty<int>("InvoiceBatchId", out var batchId))
        {
            Console.WriteLine($"    Configured context: Policy={context.PolicyName}, Tenant={context.TenantId}, BatchId={batchId}");
        }

        await executor.ExecuteAsync(
            WorkerPolicyName,
            async (ResilienceContext ctx) =>
            {
                Console.WriteLine($"    Executing job for Tenant='{ctx.TenantId}', CorrelationId='{ctx.CorrelationId}'...");
                await Task.Delay(20, ctx.CancellationToken);
            },
            context);

        Console.WriteLine(" [✓] Multi-tenant job completed successfully.");

        // 2. ResilienceContext Immutable Builder Chain API
        Console.WriteLine("\n --- 2. ResilienceContext Immutable Builder Chain (With* Methods) ---");
        var baseCtx = ResilienceContext.Create(WorkerPolicyName);
        var enrichedCtx = baseCtx
            .WithOperationName("ProcessInvoiceJob")
            .WithCorrelationId("CORR-CHAIN-4400")
            .WithTenantId("TENANT-CHAINTEST")
            .WithAttemptNumber(2);

        Console.WriteLine($"    Original context  : Operation='{baseCtx.OperationName}', CorrelationId='{baseCtx.CorrelationId}', TenantId='{baseCtx.TenantId}', AttemptNumber={baseCtx.AttemptNumber}");
        Console.WriteLine($"    Enriched (chained): Operation='{enrichedCtx.OperationName}', CorrelationId='{enrichedCtx.CorrelationId}', TenantId='{enrichedCtx.TenantId}', AttemptNumber={enrichedCtx.AttemptNumber}");
        Console.WriteLine($"    [✓] Immutable: base context PolicyName unchanged='{baseCtx.PolicyName}' / enriched PolicyName='{enrichedCtx.PolicyName}'");

        // 3. Cooperative cancellation propagation demonstration
        Console.WriteLine("\n --- 2. Background Cancellation Propagation ---");
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        try
        {
            await executor.ExecuteAsync(
                WorkerPolicyName,
                async (CancellationToken ct) =>
                {
                    Console.WriteLine("    Starting long-running background task...");
                    await Task.Delay(2000, ct); // Will be canceled at 50ms
                },
                cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("    [✓] Cooperative cancellation cleanly detected without erroneous retries.");
        }

        Console.WriteLine("\n [✓] Level 5 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }
}
