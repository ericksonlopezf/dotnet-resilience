// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides quick-start demonstrations illustrating minimal dependency injection configuration and initial resilience execution.
/// </summary>
public static class Level1QuickStart
{
    private const string PolicyName = "quickstart-policy";

    /// <summary>
    /// Executes the quick-start resilience demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 1] QUICK START: DEPENDENCY INJECTION AND INITIAL EXECUTION");
        Console.WriteLine("================================================================================");

        // 1. Configure Dependency Injection container
        var services = new ServiceCollection();

        // 2. Register resilience core and Polly execution engine adapter
        services.AddEricksonLopezResilience();

        // 3. Register named resilience policy
        services.AddResiliencePolicy(PolicyName, builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(2))
                .AddRetry(opt =>
                {
                    opt.MaxRetryAttempts = 2;
                    opt.Delay = TimeSpan.FromMilliseconds(100);
                    opt.BackoffType = BackoffType.Constant;
                });
        });

        // Register application consumer service
        services.AddTransient<OrderProcessingService>();

        var serviceProvider = services.BuildServiceProvider();

        // 4. Resolve service and execute protected operations
        var service = serviceProvider.GetRequiredService<OrderProcessingService>();

        Console.WriteLine("\n -> Executing resilient query operation...");
        var orderId = await service.GetOrderSummaryAsync("ORD-98765", CancellationToken.None);
        Console.WriteLine($" [✓] Retrieved result: {orderId}");

        Console.WriteLine("\n -> Executing void action...");
        await service.NotifyDispatchAsync("ORD-98765", CancellationToken.None);
        Console.WriteLine(" [✓] Notification dispatched successfully.");

        Console.WriteLine("\n [✓] Level 1 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }

    private sealed class OrderProcessingService
    {
        private readonly IResilienceExecutor _executor;
        private int _queryAttempts;

        public OrderProcessingService(IResilienceExecutor executor)
        {
            _executor = executor;
        }

        public async ValueTask<string> GetOrderSummaryAsync(string orderCode, CancellationToken cancellationToken)
        {
            return await _executor.ExecuteAsync(
                PolicyName,
                async (ResilienceContext context) =>
                {
                    _queryAttempts++;
                    Console.WriteLine($"    [Attempt {_queryAttempts}] Querying order status for {orderCode} (Operation: {context.OperationName})...");

                    if (_queryAttempts == 1)
                    {
                        Console.WriteLine("    [!] Simulating transient socket failure...");
                        throw new System.Net.Sockets.SocketException(10054);
                    }

                    await Task.Delay(20, context.CancellationToken);
                    return $"Order {orderCode} - Status: Confirmed";
                },
                ResilienceContext.Create(PolicyName, cancellationToken),
                cancellationToken);
        }

        public async ValueTask NotifyDispatchAsync(string orderCode, CancellationToken cancellationToken)
        {
            await _executor.ExecuteAsync(
                PolicyName,
                async (CancellationToken ct) =>
                {
                    await Task.Delay(15, ct);
                    Console.WriteLine($"    Notification dispatched for {orderCode} on thread {Environment.CurrentManagedThreadId}.");
                },
                cancellationToken);
        }
    }
}
