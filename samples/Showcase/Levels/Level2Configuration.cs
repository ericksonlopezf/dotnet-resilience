// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides configuration demonstrations illustrating strategy options, custom builders, and architectural presets.
/// </summary>
public static class Level2Configuration
{
    /// <summary>
    /// Executes the configuration resilience demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 2] FULL CONFIGURATION: OPTIONS, BUILDERS AND PRESETS");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // 1. Comprehensive configuration of all strategies with lifecycle callbacks
        services.AddResiliencePolicy("exhaustive-policy", builder =>
        {
            builder
                // Strategy 1: Rate Limiter (Ingress control)
                .AddRateLimiter(options =>
                {
                    options.Name = "GlobalIngressRateLimiter";
                    options.PermitLimit = 100;
                    options.QueueLimit = 10;
                    options.Window = TimeSpan.FromSeconds(30);
                    options.OnRejected = context =>
                    {
                        Console.WriteLine($"    [RateLimiter] Rejected by quota. Retry-After: {context.RetryAfter?.TotalSeconds:F1}s.");
                        return ValueTask.CompletedTask;
                    };
                })
                // Strategy 2: Timeout (Per-attempt deadline)
                .AddTimeout(options =>
                {
                    options.Name = "OperationTimeout";
                    options.Timeout = TimeSpan.FromSeconds(5);
                    options.OnTimeout = context =>
                    {
                        Console.WriteLine($"    [Timeout] Operation '{context.ResilienceContext.OperationName}' exceeded {context.Timeout.TotalSeconds}s.");
                        return ValueTask.CompletedTask;
                    };
                })
                // Strategy 3: Retry (Exponential backoff with full jitter)
                .AddRetry(options =>
                {
                    options.Name = "ExponentialRetry";
                    options.MaxRetryAttempts = 3;
                    options.Delay = TimeSpan.FromMilliseconds(150);
                    options.MaxDelay = TimeSpan.FromSeconds(2);
                    options.BackoffType = BackoffType.ExponentialWithJitter;
                    options.ShouldHandleException = ex => ex is System.IO.IOException or TimeoutException;
                    options.OnRetry = context =>
                    {
                        Console.WriteLine($"    [Retry] Attempt #{context.AttemptNumber} after delay of {context.Delay.TotalMilliseconds:F0}ms. Cause: {context.Exception?.GetType().Name}");
                        return ValueTask.CompletedTask;
                    };
                })
                // Strategy 4: Circuit Breaker (Cascading failure prevention)
                .AddCircuitBreaker(options =>
                {
                    options.Name = "DependencyCircuitBreaker";
                    options.FailureRatio = 0.5;
                    options.MinimumThroughput = 5;
                    options.SamplingDuration = TimeSpan.FromSeconds(10);
                    options.BreakDuration = TimeSpan.FromSeconds(5);
                    options.OnCircuitOpened = context =>
                    {
                        Console.WriteLine($"    [CircuitBreaker] Circuit OPENED. Break duration: {context.BreakDuration?.TotalSeconds}s. Cause: {context.LastException?.Message}");
                        return ValueTask.CompletedTask;
                    };
                    options.OnCircuitHalfOpened = context =>
                    {
                        Console.WriteLine("    [CircuitBreaker] Circuit HALF-OPEN (Evaluating recovery with probe traffic)...");
                        return ValueTask.CompletedTask;
                    };
                    options.OnCircuitClosed = context =>
                    {
                        Console.WriteLine("    [CircuitBreaker] Circuit CLOSED (Traffic restored to normal).");
                        return ValueTask.CompletedTask;
                    };
                })
                // Strategy 5: Hedging (Speculative retry for idempotent reads in untyped pipelines)
                // Note: For true parallel speculative execution with OnHedging callbacks,
                // use ResiliencePipelineBuilder<TResult>.AddHedging(HedgingStrategyOptions<TResult>).
                .AddHedging(options =>
                {
                    options.Name = "SpeculativeHedging";
                    options.MaxHedgedAttempts = 2;
                    options.Delay = TimeSpan.FromMilliseconds(300);
                });
        });

        // 2. Standard and specialized architectural presets
        services.AddResiliencePolicy("standard-preset", builder => builder.AddStandardResilience());
        services.AddResiliencePolicy("database-preset", builder => builder.AddDatabaseResilience(timeout: TimeSpan.FromSeconds(10), maxRetries: 4));

        // 3. Source-Generated Native AOT-Safe Configuration Binding (from IConfigurationSection)
        var configurationData = new System.Collections.Generic.Dictionary<string, string?>
        {
            ["Resilience:PaymentPolicy:Timeout:TimeoutSeconds"] = "4",
            ["Resilience:PaymentPolicy:Retry:MaxRetryAttempts"] = "3",
            ["Resilience:PaymentPolicy:Retry:DelayMilliseconds"] = "150",
            ["Resilience:PaymentPolicy:Retry:BackoffType"] = "ExponentialWithJitter",
            ["Resilience:PaymentPolicy:CircuitBreaker:FailureRatio"] = "0.5",
            ["Resilience:PaymentPolicy:CircuitBreaker:MinimumThroughput"] = "8",
            ["Resilience:PaymentPolicy:CircuitBreaker:SamplingDurationSeconds"] = "20",
            ["Resilience:PaymentPolicy:CircuitBreaker:BreakDurationSeconds"] = "10",
            ["Resilience:PaymentPolicy:RateLimiter:PermitLimit"] = "200",
            ["Resilience:PaymentPolicy:RateLimiter:QueueLimit"] = "20",
            ["Resilience:PaymentPolicy:RateLimiter:WindowSeconds"] = "60",
            ["Resilience:PaymentPolicy:RateLimiter:LimiterType"] = "SlidingWindow"
        };

        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(configurationData);
        var configuration = configBuilder.Build();

        var paymentPolicySection = configuration.GetSection("Resilience:PaymentPolicy");
        services.AddResiliencePolicyFromConfiguration("config-bound-payment-policy", paymentPolicySection);

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        Console.WriteLine("\n -> Executing policy configured with database preset...");
        var rowsAffected = await executor.ExecuteAsync(
            "database-preset",
            async (ResilienceContext ctx) =>
            {
                await Task.Delay(20, ctx.CancellationToken);
                return 42;
            },
            ResilienceContext.Create("database-preset", CancellationToken.None));

        Console.WriteLine($" [✓] Rows affected: {rowsAffected}");

        Console.WriteLine("\n -> Executing policy configured from IConfigurationSection...");
        var configBoundResult = await executor.ExecuteAsync(
            "config-bound-payment-policy",
            async (CancellationToken ct) =>
            {
                await Task.Delay(15, ct);
                return "Payment policy from config executed successfully.";
            });

        Console.WriteLine($" [✓] Config-bound policy result: '{configBoundResult}'");

        Console.WriteLine("\n -> Executing policy configured with standard preset...");
        var healthCheck = await executor.ExecuteAsync(
            "standard-preset",
            async (CancellationToken ct) =>
            {
                await Task.Delay(10, ct);
                return "Healthy";
            });

        Console.WriteLine($" [✓] System health state: {healthCheck}");

        Console.WriteLine("\n [✓] Level 2 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }
}
