// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.AspNetCore.Http;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Mediator.Extensions;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Policies;
using EricksonLopez.Resilience.Polly.Registration;
using EricksonLopez.Result;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Cookbook;

/// <summary>
/// Cookbook: Official recipe collection for production scenarios.
/// Each recipe includes: Problem, Solution, Complete Code, Explanation, Best Practices, and Common Pitfalls.
/// </summary>
public static class CookbookRecipes
{
    public static async ValueTask RunAllAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [COOKBOOK] ERICKSONLOPEZ.RESILIENCE PRODUCTION RECIPES");
        Console.WriteLine("================================================================================");

        await RunRecipe1ResultRetryAsync();
        await RunRecipe2CircuitBreakerFallbackAsync();
        await RunRecipe3TransactionAndIdempotencyAsync();
        await RunRecipe4MediatorResilienceAsync();
        await RunRecipe5ResilientHttpClientAsync();
        await RunRecipe6TypedPolicyAsync();
        await RunRecipe7SlidingWindowRateLimitingAsync();
        await RunRecipe8TypedFallbackPipelineAsync();
        await RunRecipe9ConfigurationDrivenResilienceAsync();
        await RunRecipe10SpeculativeParallelHedgingAsync();
        await RunRecipe11RateLimitRejectedExceptionAsync();
        await RunRecipe12BackoffTypeVariantsAsync();
        await RunRecipe13RateLimiterTypeVariantsAsync();
        await RunRecipe14ImmutableContextChainingAsync();
        await RunRecipe15FallbackOptionsAdvancedAsync();
        await RunRecipe16DirectOptionsObjectOverloadsAsync();

        Console.WriteLine("================================================================================");
        Console.WriteLine(" [✓] ALL COOKBOOK RECIPES EXECUTED SUCCESSFULLY");
        Console.WriteLine("================================================================================\n");
    }

    #region Recipe 1
    /// <summary>
    /// Recipe 1: Retrying Asynchronous Operations Based on Domain Result<T>.
    /// </summary>
    public static async ValueTask RunRecipe1ResultRetryAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 1: Automatic Retry of Transient Errors with Result<T>");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Exception-only retries fail to detect transient failures modeled");
        Console.WriteLine("            cleanly as Result.Failure (e.g. Error.Unavailable).");
        Console.WriteLine(" [SOLUTION]: Use AddResultRetry() extension method that evaluates both runtime");
        Console.WriteLine("            exceptions and Error/Result<T> objects automatically.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("recipe1-policy", b =>
        {
            b.AddResultRetry(opt =>
            {
                opt.MaxRetryAttempts = 2;
                opt.Delay = TimeSpan.FromMilliseconds(50);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
                opt.OnRetry = ctx =>
                {
                    Console.WriteLine($"    [Recipe 1 - Retry] Retry attempt #{ctx.AttemptNumber} triggered.");
                    return ValueTask.CompletedTask;
                };
            });
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        var attempt = 0;
        var result = await executor.ExecuteAsync(
            "recipe1-policy",
            async (ResilienceContext ctx) =>
            {
                attempt++;
                if (attempt == 1)
                {
                    return Result<string>.Failure(Error.Unavailable("Svc.Busy", "Service temporarily unavailable"));
                }
                return await ValueTask.FromResult(Result<string>.Success("Data synchronized successfully"));
            },
            ResilienceContext.Create("recipe1-policy"));

        Console.WriteLine($" [EXECUTED CODE] Result: IsSuccess={result.IsSuccess}, Value='{result.Value}'");
        Console.WriteLine(" [BEST PRACTICES]: Assign ErrorRetryability.Transient to infrastructure errors.");
        Console.WriteLine(" [COMMON PITFALLS]: Retrying validation errors (Error.Validation), which wastes resources.");
    }
    #endregion

    #region Recipe 2
    /// <summary>
    /// Recipe 2: Circuit Breaker Protection and Safe Fallback.
    /// </summary>
    public static async ValueTask RunRecipe2CircuitBreakerFallbackAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 2: Cascading Failure Prevention with Circuit Breaker");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: A failing downstream dependency causes callers to exhaust sockets");
        Console.WriteLine("            and worker threads waiting for repetitive timeouts.");
        Console.WriteLine(" [SOLUTION]: Configure AddCircuitBreaker() with break duration and catch");
        Console.WriteLine("            CircuitBrokenException to supply a degraded fallback response.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("recipe2-circuit", b =>
        {
            b.AddCircuitBreaker(opt =>
            {
                opt.FailureRatio = 0.5;
                opt.MinimumThroughput = 2;
                opt.SamplingDuration = TimeSpan.FromSeconds(5);
                opt.BreakDuration = TimeSpan.FromSeconds(2);
                opt.OnCircuitOpened = ctx =>
                {
                    Console.WriteLine($"    [Recipe 2 - CircuitBreaker] Circuit OPENED! Blocking outbound traffic.");
                    return ValueTask.CompletedTask;
                };
            });
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        // Force open the circuit
        for (int i = 1; i <= 2; i++)
        {
            try
            {
                await executor.ExecuteAsync("recipe2-circuit", async ct => throw new HttpRequestException("Critical external API outage"));
            }
            catch
            {
                // Expected for tripping
            }
        }

        // Protected call with graceful fallback
        string response;
        try
        {
            response = await executor.ExecuteAsync("recipe2-circuit", async ct => await ValueTask.FromResult("Realtime response"));
        }
        catch (CircuitBrokenException)
        {
            response = "Fallback Response from Local Cache (Degraded Mode)";
        }

        Console.WriteLine($" [EXECUTED CODE] Retrieved response: '{response}'");
        Console.WriteLine(" [BEST PRACTICES]: Tune MinimumThroughput to match expected operational traffic.");
        Console.WriteLine(" [COMMON PITFALLS]: Configuring SamplingDuration too low, causing erratic circuit tripping.");
    }
    #endregion

    #region Recipe 3
    /// <summary>
    /// Recipe 3: Fresh Transactions per Attempt and Idempotency Key Preservation.
    /// </summary>
    public static async ValueTask RunRecipe3TransactionAndIdempotencyAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 3: Transactional Boundary and Idempotency across Retries");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: If a DB transaction fails due to optimistic concurrency and is retried");
        Console.WriteLine("            on the same dirty context, entity tracking becomes corrupted.");
        Console.WriteLine(" [SOLUTION]: Open a fresh Unit of Work / Transaction INSIDE the execution delegate,");
        Console.WriteLine("            preserving the same idempotency key across all attempts.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("recipe3-tx", b =>
        {
            b.AddDatabaseResilience(TimeSpan.FromSeconds(5), maxRetries: 2);
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        var idempotencyKey = "IDEM-RECIPE-9001";
        var attempt = 0;

        var result = await executor.ExecuteAsync(
            "recipe3-tx",
            async (ResilienceContext ctx) =>
            {
                attempt++;
                Console.WriteLine($"    [Recipe 3 - Attempt {attempt}] Opening NEW DB transaction. IdempotencyKey='{idempotencyKey}'");

                if (attempt == 1)
                {
                    Console.WriteLine("    [Recipe 3 - Attempt 1] Simulating optimistic concurrency conflict.");
                    Console.WriteLine("    [Recipe 3 - Attempt 1] Rolling back and discarding dirty Unit of Work.");
                    return Result<string>.Failure(Error.Infrastructure("Db.ConcurrencyConflict", "Outdated aggregate version."));
                }

                Console.WriteLine("    [Recipe 3 - Attempt 2] Reloading fresh aggregate and committing transaction.");
                return await ValueTask.FromResult(Result<string>.Success("Transaction committed successfully"));
            },
            ResilienceContext.Create("recipe3-tx"));

        Console.WriteLine($" [EXECUTED CODE] Result: IsSuccess={result.IsSuccess}, Message='{result.Value}'");
        Console.WriteLine(" [BEST PRACTICES]: Never wrap the retry policy invocation inside a 'using var tx' block.");
        Console.WriteLine(" [COMMON PITFALLS]: Reusing a failed DbContext/Connection without resetting ChangeTracker.");
    }
    #endregion

    #region Recipe 4
    /// <summary>
    /// Recipe 4: Decoupled Mediator Integration via IResilientRequest.
    /// </summary>
    public static async ValueTask RunRecipe4MediatorResilienceAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 4: Resilient Pipeline Behavior with Mediator and IPipelineBehavior");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Scattering IResilienceExecutor calls across individual handlers");
        Console.WriteLine("            pollutes application business logic.");
        Console.WriteLine(" [SOLUTION]: Implement IResilientRequest on the command and register");
        Console.WriteLine("            services.AddResiliencePipelineBehavior() in the Mediator pipeline.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("create-customer-policy", b => b.AddStandardResilience());
        services.AddResiliencePipelineBehavior();

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();
        var behavior = new ResiliencePipelineBehavior<CreateCustomerCommand, Result<string>>(executor);
        var handler = new CreateCustomerHandler();

        var command = new CreateCustomerCommand("Acme Corp Ltd.", "create-customer-policy");
        var continuation = new StructNext<Result<string>>(() => handler.Handle(command, CancellationToken.None));
        var response = await behavior.Handle(command, continuation, CancellationToken.None);

        Console.WriteLine($" [EXECUTED CODE] Mediator response: {response.Value}");
        Console.WriteLine(" [BEST PRACTICES]: Declare policy names in shared architectural constants.");
        Console.WriteLine(" [COMMON PITFALLS]: Forgetting to register the named policy in the ServiceCollection.");
    }

    private readonly struct StructNext<T> : INext<T>
    {
        private readonly Func<ValueTask<T>> _cb;
        public StructNext(Func<ValueTask<T>> cb) => _cb = cb;
        public ValueTask<T> InvokeAsync() => _cb();
    }

    private sealed record CreateCustomerCommand(string Name, string ResiliencePolicy) : ICommand<Result<string>>, IResilientRequest;

    private sealed class CreateCustomerHandler
    {
        public ValueTask<Result<string>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($"    [Handler] Creating customer '{request.Name}' protected by policy '{request.ResiliencePolicy}'.");
            return ValueTask.FromResult(Result<string>.Success("Customer CUST-2026 registered"));
        }
    }
    #endregion

    #region Recipe 5
    /// <summary>
    /// Recipe 5: Typed HttpClient Configuration with DelegatingHandler and Observability.
    /// </summary>
    public static async ValueTask RunRecipe5ResilientHttpClientAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 5: HttpClient with Resilient Delegating Handler and OpenTelemetry");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Manually retrying HTTP calls across REST endpoints creates duplication.");
        Console.WriteLine(" [SOLUTION]: Use builder.AddResiliencePolicy() on IHttpClientBuilder.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("billing-api-policy", b =>
        {
            b.AddTimeout(TimeSpan.FromSeconds(2));
            b.AddRetry(opt => { opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(50); });
        });

        services.AddHttpClient("BillingClient", c => { c.BaseAddress = new Uri("https://billing.example.com/"); })
                .AddResiliencePolicy("billing-api-policy");

        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("BillingClient");

        Console.WriteLine($" [EXECUTED CODE] HttpClient '{client.BaseAddress}' configured with ResilienceDelegatingHandler.");
        Console.WriteLine(" [BEST PRACTICES]: Always combine with ResilienceMeter telemetry to track endpoint latency.");
        Console.WriteLine(" [COMMON PITFALLS]: Configuring retry policies on non-idempotent HTTP POST/PATCH calls without idempotency keys.");
        await Task.CompletedTask;
    }
    #endregion

    #region Recipe 6
    /// <summary>
    /// Recipe 6: Reusable Strongly-Typed Resilience Policy Definitions.
    /// </summary>
    public static async ValueTask RunRecipe6TypedPolicyAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 6: Strongly-Typed Resilience Policies");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Magic strings for policy names can lead to runtime typos and misconfigurations.");
        Console.WriteLine(" [SOLUTION]: Inherit from ResiliencePolicy and register via services.AddResiliencePolicy<T>().");

        var services = new ServiceCollection();
        services.AddResiliencePolicy<ReportingServicePolicy>();

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        var report = await executor.ExecuteAsync(
            ReportingServicePolicy.PolicyName,
            async ct => await ValueTask.FromResult("Consolidated enterprise report generated"));

        Console.WriteLine($" [EXECUTED CODE] Generated report: '{report}'");
        Console.WriteLine(" [BEST PRACTICES]: Encapsulate complex strategy options in policy classes to standardize teams.");
        Console.WriteLine(" [COMMON PITFALLS]: Duplicating policy configuration across separate microservice repositories.");
    }

    private sealed class ReportingServicePolicy : ResiliencePolicy
    {
        public const string PolicyName = "ReportingServicePolicy";
        public override string Name => PolicyName;
        public override void Configure(IResiliencePipelineBuilder builder)
        {
            builder.AddTimeout(TimeSpan.FromSeconds(10))
                   .AddRetry(opt => { opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(100); });
        }
    }
    #endregion

    #region Recipe 7
    /// <summary>
    /// Recipe 7: Sliding Window Rate Limiting and Traffic Throttling.
    /// </summary>
    public static async ValueTask RunRecipe7SlidingWindowRateLimitingAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 7: Traffic Spike Protection with Sliding Window Rate Limiter");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Sudden traffic surges overwhelm databases and downstream APIs.");
        Console.WriteLine(" [SOLUTION]: Configure AddRateLimiter() with segmented sliding window.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("sliding-rate-policy", b =>
        {
            b.AddRateLimiter(opt =>
            {
                opt.PermitLimit = 10;
                opt.QueueLimit = 2;
                opt.Window = TimeSpan.FromSeconds(1);
                opt.OnRejected = ctx =>
                {
                    Console.WriteLine($"    [Recipe 7] Request rejected due to exceeded rate limit.");
                    return ValueTask.CompletedTask;
                };
            });
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        Console.WriteLine("    Executing controlled burst of requests within 10 permit quota...");
        for (int i = 1; i <= 5; i++)
        {
            await executor.ExecuteAsync("sliding-rate-policy", async ct => await Task.Delay(5, ct));
        }

        Console.WriteLine(" [EXECUTED CODE] 5 requests admitted and processed smoothly.");
        Console.WriteLine(" [BEST PRACTICES]: Set QueueLimit > 0 only when added latency is acceptable.");
        Console.WriteLine(" [COMMON PITFALLS]: Using infinite QueueLimit, causing uncontrolled memory growth.");
    }
    #endregion

    #region Recipe 8
    /// <summary>
    /// Recipe 8: Strongly-Typed Fallback with Custom Fallback Actions.
    /// </summary>
    public static async ValueTask RunRecipe8TypedFallbackPipelineAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 8: Strongly-Typed Fallback with Custom Value Generation");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: When read-only downstream queries fail, services should gracefully");
        Console.WriteLine("            degrade rather than bubbling exceptions to the caller.");
        Console.WriteLine(" [SOLUTION]: Use ResiliencePipelineBuilder<TResult>.AddFallback() with FallbackAction.");

        PollyResilienceRegistration.RegisterTypedPipeline<string>();

        var builder = new ResiliencePipelineBuilder<string>("user-profile-query-policy")
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
                    Console.WriteLine($"    [Recipe 8 - Fallback] Operation '{ctx.Context.OperationName}' failed with {ctx.Exception?.GetType().Name}. Returning default anonymous profile.");
                    return ValueTask.FromResult("Anonymous Guest User Profile [Degraded Mode]");
                };
            });

        var pipeline = builder.Build();
        var context = ResilienceContext.Create("user-profile-query-policy");

        var profile = await pipeline.ExecuteAsync(
            async (ResilienceContext ctx) =>
            {
                Console.WriteLine("    [Recipe 8] Querying remote identity provider...");
                throw new HttpRequestException("Identity provider connection refused (ECONNREFUSED)");
            },
            context);

        Console.WriteLine($" [EXECUTED CODE] Resolved profile: '{profile}'");
        Console.WriteLine(" [BEST PRACTICES]: Only apply fallback strategies to read-only queries, never to financial commands.");
        Console.WriteLine(" [COMMON PITFALLS]: Hiding critical authorization errors behind fallback responses.");
    }
    #endregion

    #region Recipe 9
    /// <summary>
    /// Recipe 9: Configuration-Driven Resilience via appsettings.json / IConfiguration.
    /// </summary>
    public static async ValueTask RunRecipe9ConfigurationDrivenResilienceAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 9: Zero-Code Dynamic Policy Configuration from IConfiguration");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Hardcoding resilience timeouts and retry counts prevents ops teams");
        Console.WriteLine("            from tuning production policies without recompiling binaries.");
        Console.WriteLine(" [SOLUTION]: Use services.AddResiliencePolicyFromConfiguration() with IConfigurationSection.");

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Resilience:NotificationPolicy:Timeout:TimeoutSeconds"] = "3",
            ["Resilience:NotificationPolicy:Retry:MaxRetryAttempts"] = "2",
            ["Resilience:NotificationPolicy:Retry:DelayMilliseconds"] = "100",
            ["Resilience:NotificationPolicy:Retry:BackoffType"] = "ExponentialWithJitter",
            ["Resilience:NotificationPolicy:CircuitBreaker:FailureRatio"] = "0.5",
            ["Resilience:NotificationPolicy:CircuitBreaker:MinimumThroughput"] = "4",
            ["Resilience:NotificationPolicy:CircuitBreaker:SamplingDurationSeconds"] = "15",
            ["Resilience:NotificationPolicy:CircuitBreaker:BreakDurationSeconds"] = "10"
        };

        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var section = config.GetSection("Resilience:NotificationPolicy");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicyFromConfiguration("notification-service-policy", section);

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        var result = await executor.ExecuteAsync(
            "notification-service-policy",
            async (CancellationToken ct) =>
            {
                await Task.Delay(10, ct);
                return "SMS notification queued successfully.";
            });

        Console.WriteLine($" [EXECUTED CODE] Execution result from config-driven policy: '{result}'");
        Console.WriteLine(" [BEST PRACTICES]: Structure configuration sections by policy name and strategy keys.");
        Console.WriteLine(" [COMMON PITFALLS]: Supplying negative timeouts or failure ratios outside (0.0, 1.0].");
    }
    #endregion

    #region Recipe 10
    /// <summary>
    /// Recipe 10: Speculative Parallel Hedging for Idempotent Queries.
    /// </summary>
    public static async ValueTask RunRecipe10SpeculativeParallelHedgingAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 10: Speculative Parallel Hedging for Idempotent Reads");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Tail latency spikes across microservice replicas degrade P99 response times.");
        Console.WriteLine(" [SOLUTION]: Configure typed hedging via ResiliencePipelineBuilder<TResult>.AddHedging()");
        Console.WriteLine("            to launch parallel speculative attempts if the primary attempt is delayed.");

        PollyResilienceRegistration.RegisterTypedPipeline<string>();

        var builder = new ResiliencePipelineBuilder<string>("geo-dns-lookup-policy")
            .AddTimeout(TimeSpan.FromSeconds(3))
            .AddHedging(opt =>
            {
                opt.MaxHedgedAttempts = 2;
                opt.Delay = TimeSpan.FromMilliseconds(200);
                opt.OnHedging = ctx =>
                {
                    Console.WriteLine($"    [Recipe 10 - Hedging] Hedged attempt #{ctx.AttemptNumber} launched in parallel.");
                    return ValueTask.CompletedTask;
                };
            });

        var pipeline = builder.Build();
        var context = ResilienceContext.Create("geo-dns-lookup-policy");

        var dnsResult = await pipeline.ExecuteAsync(
            async (ResilienceContext ctx) =>
            {
                Console.WriteLine("    [Recipe 10 - Attempt 1] Querying Primary DNS resolver (simulating 50ms fast response)...");
                await Task.Delay(50, ctx.CancellationToken);
                return "192.168.1.100 (Resolved by Primary DNS)";
            },
            context);

        Console.WriteLine($" [EXECUTED CODE] Resolved DNS address: '{dnsResult}'");
        Console.WriteLine(" [BEST PRACTICES]: Hedging must strictly only be used for idempotent read queries.");
        Console.WriteLine(" [COMMON PITFALLS]: Applying hedging to state-modifying POST/PUT operations, causing duplicate side effects.");
    }
    #endregion

    #region Recipe 11
    /// <summary>
    /// Recipe 11: RateLimitRejectedException — Handling quota exhaustion with Retry-After.
    /// </summary>
    public static async ValueTask RunRecipe11RateLimitRejectedExceptionAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 11: Handling RateLimitRejectedException with Retry-After");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Rate limiter rejects requests silently, leaving callers without context.");
        Console.WriteLine(" [SOLUTION]: Catch RateLimitRejectedException and inspect RetryAfter for back-off.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("recipe11-rate", b =>
        {
            b.AddRateLimiter(opt =>
            {
                opt.PermitLimit = 1;
                opt.QueueLimit = 0;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.OnRejected = ctx =>
                {
                    Console.WriteLine($"    [Recipe 11 - RateLimiter] Quota exhausted. Retry-After: {ctx.RetryAfter?.TotalSeconds:F0}s");
                    return ValueTask.CompletedTask;
                };
            });
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        // Consume the single permit
        await executor.ExecuteAsync("recipe11-rate", async ct => await Task.CompletedTask);

        // Second call should be rejected
        string response;
        try
        {
            response = await executor.ExecuteAsync("recipe11-rate", async ct => await ValueTask.FromResult("Processed"));
        }
        catch (RateLimitRejectedException rlEx)
        {
            response = $"Rejected (PolicyName={rlEx.PolicyName}, RetryAfter={rlEx.RetryAfter?.TotalSeconds}s)";
        }

        Console.WriteLine($" [EXECUTED CODE] Outcome: '{response}'");
        Console.WriteLine(" [BEST PRACTICES]: Always expose Retry-After in API responses (HTTP 429).");
        Console.WriteLine(" [COMMON PITFALLS]: Setting QueueLimit > 0 when added queueing latency is unacceptable.");
        await Task.CompletedTask;
    }
    #endregion

    #region Recipe 12
    /// <summary>
    /// Recipe 12: All BackoffType Variants — Constant, Linear, Exponential, ExponentialWithJitter.
    /// </summary>
    public static async ValueTask RunRecipe12BackoffTypeVariantsAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 12: BackoffType Variants — Constant, Linear, Exponential, ExponentialWithJitter");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Teams misconfigure backoff strategies, causing thundering herds.");
        Console.WriteLine(" [SOLUTION]: Document all BackoffType values with appropriate use cases.");

        Console.WriteLine();
        Console.WriteLine($"    BackoffType.Constant            = {BackoffType.Constant}  (Fixed delay: ideal for idempotent API retries with known safe intervals)");
        Console.WriteLine($"    BackoffType.Linear              = {BackoffType.Linear}  (Linearly increasing: moderate load distribution across retries)");
        Console.WriteLine($"    BackoffType.Exponential         = {BackoffType.Exponential}  (2^n delay: aggressive back-off for overloaded services)");
        Console.WriteLine($"    BackoffType.ExponentialWithJitter= {BackoffType.ExponentialWithJitter}  (Default: anti-thundering-herd for distributed systems)");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // Constant: same fixed delay between every attempt
        services.AddResiliencePolicy("backoff-constant", b => b.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(50); opt.BackoffType = BackoffType.Constant;
            opt.OnRetry = ctx => { Console.WriteLine($"    [Constant] Attempt #{ctx.AttemptNumber}, Delay={ctx.Delay.TotalMilliseconds:F0}ms"); return ValueTask.CompletedTask; };
        }));

        // Linear: grows proportionally to attempt number
        services.AddResiliencePolicy("backoff-linear", b => b.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(50); opt.BackoffType = BackoffType.Linear;
            opt.OnRetry = ctx => { Console.WriteLine($"    [Linear] Attempt #{ctx.AttemptNumber}, Delay={ctx.Delay.TotalMilliseconds:F0}ms"); return ValueTask.CompletedTask; };
        }));

        // Exponential: 2^n * base delay
        services.AddResiliencePolicy("backoff-exponential", b => b.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(50); opt.BackoffType = BackoffType.Exponential;
            opt.OnRetry = ctx => { Console.WriteLine($"    [Exponential] Attempt #{ctx.AttemptNumber}, Delay={ctx.Delay.TotalMilliseconds:F0}ms"); return ValueTask.CompletedTask; };
        }));

        // ExponentialWithJitter (default)
        services.AddResiliencePolicy("backoff-jitter", b => b.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(50); opt.BackoffType = BackoffType.ExponentialWithJitter;
            opt.OnRetry = ctx => { Console.WriteLine($"    [ExponentialWithJitter] Attempt #{ctx.AttemptNumber}, Delay={ctx.Delay.TotalMilliseconds:F0}ms"); return ValueTask.CompletedTask; };
        }));

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();
        var attempt = 0;
        Func<ResilienceContext, ValueTask<string>> op = async ctx =>
        {
            attempt++;
            if (attempt < 3) throw new TimeoutException("Simulated transient failure");
            attempt = 0;
            return await ValueTask.FromResult("success");
        };

        foreach (var policy in new[] { "backoff-constant", "backoff-linear", "backoff-exponential", "backoff-jitter" })
        {
            await executor.ExecuteAsync(policy, op, ResilienceContext.Create(policy));
        }

        Console.WriteLine(" [EXECUTED CODE] All 4 BackoffType variants executed successfully.");
        Console.WriteLine(" [BEST PRACTICES]: Use ExponentialWithJitter in distributed systems to prevent correlated retries.");
        Console.WriteLine(" [COMMON PITFALLS]: Using Constant backoff under high concurrent load — leads to synchronized retry storms.");
    }
    #endregion

    #region Recipe 13
    /// <summary>
    /// Recipe 13: RateLimiterType Variants — SlidingWindow, FixedWindow, TokenBucket, Concurrency.
    /// </summary>
    public static async ValueTask RunRecipe13RateLimiterTypeVariantsAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 13: RateLimiterType Variants — SlidingWindow, FixedWindow, TokenBucket, Concurrency");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Choosing the wrong rate limiter causes over/under-throttling.");
        Console.WriteLine(" [SOLUTION]: Select RateLimiterType per traffic pattern.");

        Console.WriteLine();
        Console.WriteLine($"    RateLimiterType.SlidingWindow = {RateLimiterType.SlidingWindow} (Smooth distribution, no burst boundary spikes)");
        Console.WriteLine($"    RateLimiterType.FixedWindow   = {RateLimiterType.FixedWindow} (Simple window reset — may allow double-rate at boundary)");
        Console.WriteLine($"    RateLimiterType.TokenBucket   = {RateLimiterType.TokenBucket} (Burst-tolerant, steady refill — API gateways)");
        Console.WriteLine($"    RateLimiterType.Concurrency   = {RateLimiterType.Concurrency} (Limits simultaneous executions — DB connection pools)");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        services.AddResiliencePolicy("rate-sliding", b => b.AddRateLimiter(o => { o.LimiterType = RateLimiterType.SlidingWindow; o.PermitLimit = 100; o.Window = TimeSpan.FromSeconds(1); }));
        services.AddResiliencePolicy("rate-fixed", b => b.AddRateLimiter(o => { o.LimiterType = RateLimiterType.FixedWindow; o.PermitLimit = 100; o.Window = TimeSpan.FromSeconds(1); }));
        services.AddResiliencePolicy("rate-token", b => b.AddRateLimiter(o => { o.LimiterType = RateLimiterType.TokenBucket; o.PermitLimit = 100; o.Window = TimeSpan.FromSeconds(1); }));
        services.AddResiliencePolicy("rate-concurrency", b => b.AddRateLimiter(o => { o.LimiterType = RateLimiterType.Concurrency; o.PermitLimit = 50; o.QueueLimit = 5; o.Window = TimeSpan.FromSeconds(1); }));

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        foreach (var policy in new[] { "rate-sliding", "rate-fixed", "rate-token", "rate-concurrency" })
        {
            await executor.ExecuteAsync(policy, async ct => await Task.CompletedTask);
            Console.WriteLine($"    [\u2713] RateLimiterType '{policy}' executed successfully.");
        }

        Console.WriteLine(" [EXECUTED CODE] All 4 RateLimiterType variants executed successfully.");
        Console.WriteLine(" [BEST PRACTICES]: Use Concurrency limiter to protect finite resources (DB pools, file handles).");
        Console.WriteLine(" [COMMON PITFALLS]: Using FixedWindow on microservices with burst traffic — spike at window boundaries.");
    }
    #endregion

    #region Recipe 14
    /// <summary>
    /// Recipe 14: ResilienceContext Immutable Builder Chain (WithOperationName, WithCorrelationId, WithTenantId, WithAttemptNumber).
    /// </summary>
    public static ValueTask RunRecipe14ImmutableContextChainingAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 14: Immutable ResilienceContext Builder Chain");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Mutating a shared ResilienceContext across retries causes dirty state.");
        Console.WriteLine(" [SOLUTION]: Each With* method returns a new isolated instance, preserving immutability.");

        var original = ResilienceContext.Create("my-policy");
        var enriched = original
            .WithOperationName("FetchUserProfile")
            .WithCorrelationId("CORR-20260101")
            .WithTenantId("TENANT-ACME")
            .WithAttemptNumber(1)
            .SetProperty("RequestId", "REQ-7799");

        Console.WriteLine($"    Original : OperationName='{original.OperationName}', TenantId='{original.TenantId}'");
        Console.WriteLine($"    Enriched : OperationName='{enriched.OperationName}', TenantId='{enriched.TenantId}', AttemptNumber={enriched.AttemptNumber}");

        enriched.TryGetProperty<string>("RequestId", out var reqId);
        Console.WriteLine($"    Property : RequestId='{reqId}'");

        Console.WriteLine(" [EXECUTED CODE] Context chain preserved immutability successfully.");
        Console.WriteLine(" [BEST PRACTICES]: Pass enriched context through the entire call chain for full distributed trace correlation.");
        Console.WriteLine(" [COMMON PITFALLS]: Calling SetProperty on a context shared across parallel tasks causes race conditions.");
        return ValueTask.CompletedTask;
    }
    #endregion

    #region Recipe 15
    /// <summary>
    /// Recipe 15: Advanced FallbackStrategyOptions<TResult> — OnFallback, ShouldHandleException, ShouldHandleResult.
    /// </summary>
    public static async ValueTask RunRecipe15FallbackOptionsAdvancedAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 15: Advanced Fallback — OnFallback Callback and Conditional Triggers");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: Default fallback triggers on any exception, including non-transient errors.");
        Console.WriteLine(" [SOLUTION]: Configure ShouldHandleException and ShouldHandleResult predicates on");
        Console.WriteLine("            FallbackStrategyOptions<TResult> to limit when fallback activates.");

        PollyResilienceRegistration.RegisterTypedPipeline<string>();

        var pipeline = new ResiliencePipelineBuilder<string>("advanced-fallback")
            .AddFallback(opt =>
            {
                // Only activate fallback on network-related exceptions
                opt.ShouldHandleException = ex => ex is HttpRequestException or TimeoutException;

                // Only activate fallback when result indicates failure
                opt.ShouldHandleResult = r => r == null || r.StartsWith("ERROR:", StringComparison.Ordinal);

                opt.OnFallback = ctx =>
                {
                    Console.WriteLine($"    [Recipe 15 - OnFallback] Fallback triggered. Exception='{ctx.Exception?.GetType().Name}'");
                    return ValueTask.CompletedTask;
                };

                opt.FallbackAction = ctx =>
                {
                    return ValueTask.FromResult("Cached Response v1.2 [Fallback]");
                };
            })
            .Build();

        // Scenario 1: Exception that matches ShouldHandleException → triggers fallback
        var fallbackResult = await pipeline.ExecuteAsync(
            async ctx =>
            {
                Console.WriteLine("    [Attempt] Querying primary source (will throw HttpRequestException)...");
                throw new HttpRequestException("Primary endpoint unreachable");
            },
            ResilienceContext.Create("advanced-fallback"));

        Console.WriteLine($" [EXECUTED CODE] Fallback result (exception path): '{fallbackResult}'");

        // Scenario 2: Result that matches ShouldHandleResult → triggers fallback
        var resultFallback = await pipeline.ExecuteAsync(
            async ctx =>
            {
                Console.WriteLine("    [Attempt] Service returned error status...");
                return await ValueTask.FromResult("ERROR: ServiceDegraded");
            },
            ResilienceContext.Create("advanced-fallback"));

        Console.WriteLine($" [EXECUTED CODE] Fallback result (result predicate path): '{resultFallback}'");
        Console.WriteLine(" [BEST PRACTICES]: Combine ShouldHandleException with ShouldHandleResult for precise fallback control.");
        Console.WriteLine(" [COMMON PITFALLS]: Catching all exceptions in fallback hides critical contract violations.");
    }
    #endregion

    #region Recipe 16
    /// <summary>
    /// Recipe 16: Direct Options-Object Overloads for AddRateLimiter and AddHedging (untyped).
    /// </summary>
    public static async ValueTask RunRecipe16DirectOptionsObjectOverloadsAsync()
    {
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" RECIPE 16: Direct Options-Object Overloads — AddRateLimiter and AddHedging");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine(" [PROBLEM]: When sharing strategy options across multiple builders or when pre-configuring");
        Console.WriteLine("            options before registration, the action-delegate overload forces recreation.");
        Console.WriteLine(" [SOLUTION]: Use the direct options-object overloads AddRateLimiter(options) and");
        Console.WriteLine("            AddHedging(options) to pass pre-constructed, pre-validated strategy instances.");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // Pre-construct and share RateLimiterStrategyOptions
        var sharedRateLimiterOptions = new RateLimiterStrategyOptions
        {
            Name = "SharedRateLimiter",
            PermitLimit = 50,
            QueueLimit = 5,
            Window = TimeSpan.FromSeconds(10),
            LimiterType = RateLimiterType.TokenBucket,
            OnRejected = ctx =>
            {
                Console.WriteLine($"    [Recipe 16 - RateLimiter] Shared options rejected call. RetryAfter={ctx.RetryAfter?.TotalSeconds:F0}s");
                return ValueTask.CompletedTask;
            }
        };

        // Pre-construct and share HedgingStrategyOptions (untyped sequential hedging)
        var sharedHedgingOptions = new HedgingStrategyOptions
        {
            Name = "SharedHedging",
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            ShouldHandleException = ex => ex is TimeoutException
        };

        // Register policy using direct options-object overloads
        services.AddResiliencePolicy("recipe16-shared-options", builder =>
        {
            builder
                .AddRateLimiter(sharedRateLimiterOptions)  // direct options-object overload
                .AddTimeout(TimeSpan.FromSeconds(5))
                .AddRetry(new RetryStrategyOptions                  // direct options-object overload
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.FromMilliseconds(50),
                    BackoffType = BackoffType.Linear
                })
                .AddHedging(sharedHedgingOptions);        // direct options-object overload
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        Console.WriteLine("    Executing operation with shared, pre-constructed options...");
        var result = await executor.ExecuteAsync(
            "recipe16-shared-options",
            async (CancellationToken ct) =>
            {
                await Task.Delay(10, ct);
                return "Processed with pre-constructed shared strategy options.";
            });

        Console.WriteLine($" [EXECUTED CODE] Result: '{result}'");
        Console.WriteLine(" [BEST PRACTICES]: Pre-construct and validate options once; pass the same instance to multiple pipelines.");
        Console.WriteLine(" [COMMON PITFALLS]: Mutating shared options objects after passing them to builders — options are not copied.");
    }
    #endregion
}
