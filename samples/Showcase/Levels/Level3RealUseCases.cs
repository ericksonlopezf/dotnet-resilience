// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides real-world use case demonstrations illustrating production scenarios with Result pattern integration and deterministic classification.
/// </summary>
public static class Level3RealUseCases
{
    private const string PaymentGatewayPolicy = "payment-gateway";

    /// <summary>
    /// Executes the real-world use cases demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 3] REAL USE CASES: RESULT PATTERN AND ENTERPRISE RESILIENCE");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // Payment Gateway Policy: Result<T> based retries
        services.AddResiliencePolicy(PaymentGatewayPolicy, builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(3))
                .AddResultRetry(opt =>
                {
                    opt.MaxRetryAttempts = 3;
                    opt.Delay = TimeSpan.FromMilliseconds(80);
                    opt.BackoffType = BackoffType.ExponentialWithJitter;
                    opt.OnRetry = ctx =>
                    {
                        Console.WriteLine($"    [PaymentRetry] Retrying payment after transient failure (#{ctx.AttemptNumber})...");
                        return ValueTask.CompletedTask;
                    };
                });
        });

        // Inventory Policy with Circuit Breaker
        services.AddResiliencePolicy("inventory-service", builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(2))
                .AddCircuitBreaker(opt =>
                {
                    opt.FailureRatio = 0.5;
                    opt.MinimumThroughput = 3;
                    opt.SamplingDuration = TimeSpan.FromSeconds(5);
                    opt.BreakDuration = TimeSpan.FromSeconds(3);
                })
                .AddRetry(opt =>
                {
                    opt.MaxRetryAttempts = 1;
                    opt.Delay = TimeSpan.FromMilliseconds(50);
                });
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        // Scenario 1: Payment experiencing initial transient failure and recovering
        Console.WriteLine("\n --- Scenario 1: Payment with transient gateway outage (Result.Unavailable) ---");
        var paymentAttempts = 0;
        var paymentResult = await executor.ExecuteAsync(
            PaymentGatewayPolicy,
            async (ResilienceContext ctx) =>
            {
                paymentAttempts++;
                Console.WriteLine($"    [Attempt {paymentAttempts}] Processing charge for $150.00 USD...");

                if (paymentAttempts < 3)
                {
                    Console.WriteLine("    [!] Error: Gateway temporarily unavailable (503 Service Unavailable).");
                    // Return transient failure Result
                    return Result<PaymentReceipt>.Failure(Error.Unavailable("Gateway.Unavailable", "Payment gateway undergoing brief maintenance."));
                }

                await Task.Delay(10, ctx.CancellationToken);
                return Result<PaymentReceipt>.Success(new PaymentReceipt("TX-998811", 150.00m, "Approved"));
            },
            ResilienceContext.Create(PaymentGatewayPolicy));

        Console.WriteLine($" [✓] Payment Result: IsSuccess={paymentResult.IsSuccess}, TxId={paymentResult.Value?.TransactionId}");

        // Scenario 2: Payment with domain validation error (Error.Validation - NON-RETRYABLE)
        Console.WriteLine("\n --- Scenario 2: Payment with business error / insufficient funds (NON-RETRYABLE) ---");
        var validationAttempts = 0;
        var domainErrorResult = await executor.ExecuteAsync(
            PaymentGatewayPolicy,
            async (ResilienceContext ctx) =>
            {
                validationAttempts++;
                Console.WriteLine($"    [Attempt {validationAttempts}] Verifying account funds...");
                return Result<PaymentReceipt>.Failure(Error.Validation("Payment.InsufficientFunds", "Account has insufficient funds."));
            },
            ResilienceContext.Create(PaymentGatewayPolicy));

        Console.WriteLine($" [✓] Result: IsSuccess={domainErrorResult.IsSuccess}, Attempts Executed={validationAttempts} (Not retried incorrectly).");

        // Scenario 3: Circuit Breaker triggered after consecutive failures
        Console.WriteLine("\n --- Scenario 3: Circuit Breaker tripping in inventory service ---");
        for (int i = 1; i <= 4; i++)
        {
            try
            {
                Console.WriteLine($"    [Request {i}] Querying stock...");
                await executor.ExecuteAsync("inventory-service", async (CancellationToken ct) =>
                {
                    throw new System.Net.Http.HttpRequestException("Inventory service down", null, System.Net.HttpStatusCode.ServiceUnavailable);
                });
            }
            catch (CircuitBrokenException cbEx)
            {
                Console.WriteLine($"    [!] CircuitBrokenException intercepted: {cbEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [!] Handled exception: {ex.GetType().Name} - {ex.Message}");
            }
        }

        Console.WriteLine("\n [✓] Level 3 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }

    public sealed record PaymentReceipt(string TransactionId, decimal Amount, string Status);
}
