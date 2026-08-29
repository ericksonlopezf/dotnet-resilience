// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Pipelines;
using EricksonLopez.Resilience.Policies;
using EricksonLopez.Resilience.Polly.Registration;
using EricksonLopez.Resilience.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Level 8 — Customization: Strongly-Typed Policies, Custom Error Classifiers, and Decoupled Pipelines.
/// </summary>
public static class Level8Customization
{
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 8] CUSTOMIZATION: STRONGLY-TYPED POLICIES AND CUSTOM CLASSIFIERS");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // 1. Register strongly-typed policy via IResiliencePolicy interface
        services.AddResiliencePolicy<EnterprisePaymentResiliencePolicy>();

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        Console.WriteLine("\n --- 1. Execution with Typed Policy (EnterprisePaymentResiliencePolicy) ---");
        var result = await executor.ExecuteAsync(
            EnterprisePaymentResiliencePolicy.PolicyName,
            async (CancellationToken ct) =>
            {
                await Task.Delay(10, ct);
                return "Subscription payment charged successfully.";
            });

        Console.WriteLine($" [✓] Result: {result}");

        // 2. Strongly-Typed Resilience Pipeline Builder with Fallback and Parallel Hedging
        Console.WriteLine("\n --- 2. Strongly-Typed Pipeline Builder (ResiliencePipelineBuilder<TResult>) with Fallback ---");
        PollyResilienceRegistration.RegisterTypedPipeline<string>();

        var typedBuilder = new ResiliencePipelineBuilder<string>("CatalogSearchPipeline")
            .AddTimeout(TimeSpan.FromSeconds(2))
            .AddRetry(opt =>
            {
                opt.MaxRetryAttempts = 1;
                opt.Delay = TimeSpan.FromMilliseconds(50);
            })
            .AddFallback(opt =>
            {
                opt.FallbackAction = ctx =>
                {
                    Console.WriteLine($"    [Fallback Triggered] Generating cached fallback response for '{ctx.Context.OperationName}'...");
                    return ValueTask.FromResult("Cached Catalog Results [Offline Mode]");
                };
            });

        var typedPipeline = typedBuilder.Build();
        var searchContext = ResilienceContext.Create("CatalogSearchPipeline");

        var searchResult = await typedPipeline.ExecuteAsync(
            async (ResilienceContext ctx) =>
            {
                Console.WriteLine("    Simulating primary catalog service network outage...");
                throw new System.Net.Sockets.SocketException(10054);
            },
            searchContext);

        Console.WriteLine($" [✓] Typed pipeline result with fallback: '{searchResult}'");

        // 3. Registering and resolving Typed Pipelines in ResiliencePipelineRegistry
        Console.WriteLine("\n --- 3. Typed Pipeline Registry (IResiliencePipelineRegistry.GetPipeline<TResult>) ---");
        var registry = new ResiliencePipelineRegistry();
        registry.Register("catalog-search", typedPipeline);

        if (registry.TryGetPipeline<string>("catalog-search", out var resolvedTypedPipeline))
        {
            var fallbackQuery = await resolvedTypedPipeline.ExecuteAsync(
                async ct => throw new TimeoutException("Database query timeout"),
                CancellationToken.None);
            Console.WriteLine($" [✓] Resolved from registry & executed: '{fallbackQuery}'");
        }

        // 4. Direct usage of PassthroughResiliencePipeline for unit testing without external dependencies
        Console.WriteLine("\n --- 4. PassthroughResiliencePipeline Demonstration (Ideal for Unit Testing) ---");
        IResiliencePipeline testPipeline = new PassthroughResiliencePipeline("UnitTestingPipeline");
        var testResult = await testPipeline.ExecuteAsync(
            async (CancellationToken ct) =>
            {
                await Task.Delay(5, ct);
                return "Execution without strategy overhead.";
            });

        Console.WriteLine($" [✓] Passthrough pipeline result: {testResult}");

        // 4b. Typed PassthroughResiliencePipeline<TResult>
        Console.WriteLine("\n --- 4b. PassthroughResiliencePipeline<TResult> (Typed No-Op for Unit Tests) ---");
        IResiliencePipeline<string> typedTestPipeline = new PassthroughResiliencePipeline<string>("TypedUnitTestingPipeline");
        var typedTestResult = await typedTestPipeline.ExecuteAsync(
            async (ResilienceContext ctx) =>
            {
                await Task.Delay(5, ctx.CancellationToken);
                return $"Typed passthrough result from '{ctx.PolicyName}'.";
            },
            ResilienceContext.Create("TypedUnitTestingPipeline"));

        Console.WriteLine($" [✓] Typed passthrough pipeline result: {typedTestResult}");

        // 4c. ResiliencePolicyRegistry — policy definition registry (pre-compilation stage)
        Console.WriteLine("\n --- 4c. ResiliencePolicyRegistry (Policy Definition Registry) ---");
        var policyRegistry = new ResiliencePolicyRegistry();
        policyRegistry.Register(new EnterprisePaymentResiliencePolicy());

        if (policyRegistry.TryGetPolicy(EnterprisePaymentResiliencePolicy.PolicyName, out var foundPolicy))
        {
            Console.WriteLine($"    [✓] Found policy definition: Name='{foundPolicy.Name}', Type={foundPolicy.GetType().Name}");
        }

        Console.WriteLine($"    Registered policies count: {policyRegistry.Policies.Count}");

        // 5. Custom Result Classifier Demonstration
        Console.WriteLine("\n --- 5. Custom Error Classifier ---");
        IResultRetryClassifier customClassifier = new CustomDomainErrorClassifier();
        var customEx = new InvalidOperationException("LOCK_TIMEOUT");
        var decision = customClassifier.ClassifyException(customEx);
        Console.WriteLine($"    Exception '{customEx.Message}' classified by CustomClassifier as: {decision}");

        // 6. Custom RateLimiter injection via RateLimiterStrategyOptions.CustomRateLimiter
        // This overrides LimiterType and all other built-in parameters with a custom RateLimiter instance.
        Console.WriteLine("\n --- 6. CustomRateLimiter Injection (RateLimiterStrategyOptions.CustomRateLimiter) ---");

        // Create a custom ConcurrencyLimiter: max 3 concurrent executions, queue up to 2
        var customConcurrencyLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = 3,
            QueueLimit = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        var services2 = new ServiceCollection();
        services2.AddEricksonLopezResilience();
        services2.AddResiliencePolicy("custom-limiter-policy", builder =>
        {
            builder.AddRateLimiter(opt =>
            {
                // Injecting a completely custom RateLimiter bypasses LimiterType, PermitLimit, QueueLimit, Window
                opt.CustomRateLimiter = customConcurrencyLimiter;
                opt.OnRejected = context =>
                {
                    Console.WriteLine("    [CustomRateLimiter] Execution rejected by custom concurrency limiter.");
                    return ValueTask.CompletedTask;
                };
            });
        });

        var sp2 = services2.BuildServiceProvider();
        var executor2 = sp2.GetRequiredService<IResilienceExecutor>();

        Console.WriteLine("    Executing 3 operations within custom concurrency limiter (limit: 3 concurrent)...");
        for (int i = 1; i <= 3; i++)
        {
            var opIndex = i;
            var opResult = await executor2.ExecuteAsync(
                "custom-limiter-policy",
                async (CancellationToken ct) =>
                {
                    await Task.Delay(10, ct);
                    return $"Operation {opIndex} completed.";
                });
            Console.WriteLine($"    [✓] {opResult}");
        }

        Console.WriteLine($"    Custom ConcurrencyLimiter stats: PermitLimit=3, QueueLimit=2");
        await customConcurrencyLimiter.DisposeAsync();

        Console.WriteLine("\n [✓] Level 8 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }

    /// <summary>
    /// Strongly-typed policy reusable across the enterprise architecture.
    /// </summary>
    public sealed class EnterprisePaymentResiliencePolicy : ResiliencePolicy
    {
        public const string PolicyName = "EnterprisePaymentPolicy";

        public override string Name => PolicyName;

        public override void Configure(IResiliencePipelineBuilder builder)
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(5))
                .AddRetry(opt =>
                {
                    opt.MaxRetryAttempts = 3;
                    opt.Delay = TimeSpan.FromMilliseconds(100);
                    opt.BackoffType = BackoffType.ExponentialWithJitter;
                })
                .AddCircuitBreaker(opt =>
                {
                    opt.FailureRatio = 0.5;
                    opt.MinimumThroughput = 10;
                    opt.SamplingDuration = TimeSpan.FromSeconds(30);
                    opt.BreakDuration = TimeSpan.FromSeconds(15);
                });
        }
    }

    /// <summary>
    /// Custom classifier evaluating specific error codes or domain exception messages.
    /// </summary>
    private sealed class CustomDomainErrorClassifier : IResultRetryClassifier
    {
        public RetryabilityDecision ClassifyResult<T>(T result) => RetryabilityDecision.Undetermined;

        public RetryabilityDecision ClassifyException(Exception exception)
        {
            if (exception is InvalidOperationException inv && inv.Message.Contains("LOCK_TIMEOUT", StringComparison.Ordinal))
            {
                return RetryabilityDecision.Retry;
            }

            return RetryabilityDecision.DoNotRetry;
        }
    }
}
