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
/// Provides advanced integration demonstrations illustrating mediator pipeline behaviors, transactional boundaries, and idempotency.
/// </summary>
public static class Level4AdvancedIntegration
{
    /// <summary>
    /// Executes the advanced integration resilience demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
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
    /// Represents a zero-allocation continuation struct compatible with Native AOT.
    /// </summary>
    /// <typeparam name="T">The type of value returned by the continuation.</typeparam>
    public readonly struct StructContinuation<T> : INext<T>
    {
        private readonly Func<ValueTask<T>> _callback;

        /// <summary>
        /// Initializes a new instance of the <see cref="StructContinuation{T}"/> struct.
        /// </summary>
        /// <param name="callback">The asynchronous callback delegate to invoke.</param>
        public StructContinuation(Func<ValueTask<T>> callback) => _callback = callback;

        /// <inheritdoc/>
        public ValueTask<T> InvokeAsync() => _callback();
    }

    /// <summary>
    /// Represents a command implementing <see cref="IResilientRequest"/> that declares its execution policy.
    /// </summary>
    /// <param name="CustomerId">The unique identifier of the customer submitting the order.</param>
    /// <param name="Total">The total monetary amount of the order.</param>
    /// <param name="IdempotencyKey">The unique idempotency key for preventing duplicate executions.</param>
    public sealed record SubmitOrderCommand(string CustomerId, decimal Total, string IdempotencyKey)
        : ICommand<Result<Guid>>, IResilientRequest
    {
        /// <inheritdoc/>
        public string ResiliencePolicy => "order-submission-policy";
    }

    /// <summary>
    /// Provides a command handler that simulates transactional boundary delimitation per retry attempt.
    /// </summary>
    public sealed class SubmitOrderCommandHandler
    {
        private int _executionCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitOrderCommandHandler"/> class.
        /// </summary>
        public SubmitOrderCommandHandler()
        {
        }

        /// <summary>
        /// Handles the specified order submission command.
        /// </summary>
        /// <param name="request">The order command to process.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A value task representing the asynchronous operation. The task result contains the created order identifier on success, or a domain error on failure.</returns>
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
