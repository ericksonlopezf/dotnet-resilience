// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Mediator.Extensions;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides enterprise architecture demonstrations illustrating a mission-critical end-to-end clean architecture solution.
/// </summary>
public static class Level10EnterpriseArchitecture
{
    private const string PolicyName = "enterprise-checkout-policy";

    /// <summary>
    /// Executes the enterprise architecture resilience demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 10] ENTERPRISE ARCHITECTURE: END-TO-END MISSION-CRITICAL SOLUTION");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();

        // 1. Register Resilience and Observability infrastructure
        services.AddEricksonLopezResilience();

        // Enterprise Architecture Policy: Ingress Rate Limiting, Timeout, Retry with Jitter, and Circuit Breaker
        services.AddResiliencePolicy(PolicyName, builder =>
        {
            builder
                .AddRateLimiter(opt =>
                {
                    opt.PermitLimit = 500;
                    opt.QueueLimit = 50;
                    opt.Window = TimeSpan.FromMinutes(1);
                })
                .AddTimeout(TimeSpan.FromSeconds(5))
                .AddResultRetry(opt =>
                {
                    opt.MaxRetryAttempts = 3;
                    opt.Delay = TimeSpan.FromMilliseconds(100);
                    opt.BackoffType = BackoffType.ExponentialWithJitter;
                    opt.OnRetry = ctx =>
                    {
                        Console.WriteLine($"    [Checkout-Retry] Attempt #{ctx.AttemptNumber} after delay of {ctx.Delay.TotalMilliseconds:F0}ms.");
                        return ValueTask.CompletedTask;
                    };
                })
                .AddCircuitBreaker(opt =>
                {
                    opt.FailureRatio = 0.5;
                    opt.MinimumThroughput = 10;
                    opt.SamplingDuration = TimeSpan.FromSeconds(30);
                    opt.BreakDuration = TimeSpan.FromSeconds(15);
                });
        });

        // 2. Register Resilience Pipeline Behavior and Handlers
        services.AddResiliencePipelineBehavior();
        services.AddTransient<CheckoutOrderCommandHandler>();

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();
        var behavior = new ResiliencePipelineBehavior<CheckoutOrderCommand, Result<CheckoutReceipt>>(executor);
        var handler = sp.GetRequiredService<CheckoutOrderCommandHandler>();

        // 3. Incoming Presentation Layer Request Simulation (Minimal API / Controller)
        Console.WriteLine("\n -> Incoming HTTP POST /api/v1/orders/checkout received (Idempotency-Key: IDEM-ORD-2026)...");

        using (var activity = ResilienceActivitySource.StartExecutionActivity(PolicyName, "ProcessCheckout", tenantId: "CORP-ENTERPRISE", correlationId: "CORR-TX-5544"))
        {
            var command = new CheckoutOrderCommand("CUST-ENTERPRISE-01", 1250.00m, "IDEM-ORD-2026", "TENANT-CORP");
            var next = new StructContinuation<Result<CheckoutReceipt>>(() => handler.Handle(command, CancellationToken.None));
            var result = await behavior.Handle(command, next, CancellationToken.None);

            if (result.IsSuccess)
            {
                Console.WriteLine($" [✓] Checkout processed successfully: OrderId={result.Value?.OrderId}, TxId={result.Value?.PaymentRef}");
                ResilienceMeter.RecordExecution(PolicyName, "ProcessCheckout", 120.5, isSuccess: true, tenantId: "CORP-ENTERPRISE");
            }
            else
            {
                Console.WriteLine($" [!] Checkout failed: {result.Error?.Code} - {result.Error?.Description}");
            }
        }

        Console.WriteLine("\n [✓] Level 10 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }

    /// <summary>
    /// Represents a zero-allocation continuation struct compatible with Native AOT.
    /// </summary>
    /// <typeparam name="T">The type of value returned by the continuation.</typeparam>
    public readonly struct StructContinuation<T> : INext<T>
    {
        private readonly Func<ValueTask<T>> _fn;

        /// <summary>
        /// Initializes a new instance of the <see cref="StructContinuation{T}"/> struct.
        /// </summary>
        /// <param name="fn">The asynchronous callback delegate to invoke.</param>
        public StructContinuation(Func<ValueTask<T>> fn) => _fn = fn;

        /// <inheritdoc/>
        public ValueTask<T> InvokeAsync() => _fn();
    }

    /// <summary>
    /// Represents an enterprise checkout command implementing <see cref="IResilientRequest"/>.
    /// </summary>
    /// <param name="CustomerId">The unique customer identifier.</param>
    /// <param name="TotalAmount">The total checkout amount.</param>
    /// <param name="IdempotencyKey">The unique idempotency key.</param>
    /// <param name="TenantId">The tenant identifier.</param>
    public sealed record CheckoutOrderCommand(string CustomerId, decimal TotalAmount, string IdempotencyKey, string TenantId)
        : ICommand<Result<CheckoutReceipt>>, IResilientRequest
    {
        /// <inheritdoc/>
        public string ResiliencePolicy => PolicyName;
    }

    /// <summary>
    /// Represents a receipt generated upon successful checkout processing.
    /// </summary>
    /// <param name="OrderId">The unique identifier of the placed order.</param>
    /// <param name="PaymentRef">The payment reference string.</param>
    /// <param name="AmountPaid">The monetary amount paid.</param>
    /// <param name="Timestamp">The UTC timestamp when checkout was processed.</param>
    public sealed record CheckoutReceipt(Guid OrderId, string PaymentRef, decimal AmountPaid, DateTime Timestamp);

    /// <summary>
    /// Provides a command handler that processes enterprise checkout operations.
    /// </summary>
    public sealed class CheckoutOrderCommandHandler
    {
        private int _attempts;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutOrderCommandHandler"/> class.
        /// </summary>
        public CheckoutOrderCommandHandler()
        {
        }

        /// <summary>
        /// Handles the specified checkout order command.
        /// </summary>
        /// <param name="request">The checkout command to process.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A value task representing the asynchronous operation. The task result contains the checkout receipt on success, or a domain error on failure.</returns>
        public async ValueTask<Result<CheckoutReceipt>> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
        {
            _attempts++;
            Console.WriteLine($"    [Enterprise Handler Attempt {_attempts}] Delimiting transactional Unit of Work...");

            if (_attempts == 1)
            {
                Console.WriteLine("    [!] Transient failure: Lock acquisition timeout on DB row.");
                return Result<CheckoutReceipt>.Failure(Error.Infrastructure("Db.RowLockTimeout", "Database row lock wait timeout exceeded."));
            }

            Console.WriteLine($"    [✓] Idempotency record validated, aggregate saved and Outbox event published atomically.");
            await Task.Delay(25, cancellationToken);

            return Result<CheckoutReceipt>.Success(new CheckoutReceipt(
                Guid.NewGuid(),
                $"PAY-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}",
                request.TotalAmount,
                DateTime.UtcNow));
        }
    }
}
