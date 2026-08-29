// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Mediator.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Level 4 — Advanced Integration: Mediator, Pipeline Behaviors, Transactional Boundaries, and Idempotency.
/// </summary>
public static class Level4AdvancedIntegration
{
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 4] ADVANCED INTEGRATION: MEDIATOR AND TRANSACTION BOUNDARIES");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();

        // 1. Register Resilience and named policy required by request
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("order-submission-policy", builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(3))
                .AddResultRetry(opt =>
                {
                    opt.MaxRetryAttempts = 3;
                    opt.Delay = TimeSpan.FromMilliseconds(50);
                    opt.BackoffType = BackoffType.ExponentialWithJitter;
                    opt.OnRetry = ctx =>
                    {
                        Console.WriteLine($"    [MediatorRetry] Retrying command on attempt #{ctx.AttemptNumber}...");
                        return ValueTask.CompletedTask;
                    };
                });
        });

        // 2. Register ResiliencePipelineBehavior in DI
        services.AddResiliencePipelineBehavior();
        services.AddTransient<SubmitOrderCommandHandler>();

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();
        var behavior = new ResiliencePipelineBehavior<SubmitOrderCommand, Result<Guid>>(executor);
        var handler = sp.GetRequiredService<SubmitOrderCommandHandler>();

        Console.WriteLine("\n -> Dispatching mediator command with IResilientRequest through ResiliencePipelineBehavior...");
        var command = new SubmitOrderCommand("CUST-100", 299.99m, "IDEM-KEY-778899");
        var continuation = new StructContinuation<Result<Guid>>(() => handler.Handle(command, CancellationToken.None));
        var result = await behavior.Handle(command, continuation, CancellationToken.None);

        Console.WriteLine($" [✓] Command executed through pipeline. IsSuccess={result.IsSuccess}, OrderId={result.Value}");

        Console.WriteLine("\n [✓] Level 4 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }

    /// <summary>
    /// Zero-allocation continuation struct compatible with Native AOT.
    /// </summary>
    public readonly struct StructContinuation<T> : INext<T>
    {
        private readonly Func<ValueTask<T>> _callback;
        public StructContinuation(Func<ValueTask<T>> callback) => _callback = callback;
        public ValueTask<T> InvokeAsync() => _callback();
    }

    /// <summary>
    /// Command implementing IResilientRequest declaring its execution policy.
    /// </summary>
    public sealed record SubmitOrderCommand(string CustomerId, decimal Total, string IdempotencyKey)
        : ICommand<Result<Guid>>, IResilientRequest
    {
        public string ResiliencePolicy => "order-submission-policy";
    }

    /// <summary>
    /// Handler simulating clean transactional boundary delimitation per retry attempt.
    /// </summary>
    public sealed class SubmitOrderCommandHandler
    {
        private int _executionCount;

        public async ValueTask<Result<Guid>> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
        {
            _executionCount++;
            Console.WriteLine($"    [Handler Attempt {_executionCount}] Starting DB transaction for IdempotencyKey='{request.IdempotencyKey}'...");

            if (_executionCount == 1)
            {
                Console.WriteLine("    [!] Simulating transient DB deadlock (SQL Error 1205)...");
                Console.WriteLine("    [!] Aborting and rolling back transaction from attempt 1.");
                return Result<Guid>.Failure(Error.Infrastructure("Database.Deadlock", "Transient deadlock conflict detected."));
            }

            Console.WriteLine("    [✓] Transaction committed and atomically recorded into Outbox.");
            await Task.Delay(10, cancellationToken);
            return Result<Guid>.Success(Guid.NewGuid());
        }
    }
}
