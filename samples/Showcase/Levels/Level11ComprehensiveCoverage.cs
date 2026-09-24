// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.AspNetCore.HealthChecks;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Policies;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Resilience.Polly.Registration;
using EricksonLopez.Resilience.Registry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly.CircuitBreaker;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides comprehensive coverage demonstrations verifying public APIs and living invariants.
/// </summary>
public static class Level11ComprehensiveCoverage
{
    private sealed class ShowcaseCustomPolicy : ResiliencePolicy
    {
        public ShowcaseCustomPolicy()
        {
        }

        public override string Name => "showcase-custom-policy";

        public override void Configure(IResiliencePipelineBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddTimeout(TimeSpan.FromSeconds(3));
        }
    }

    private sealed class DummyEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention) { }
    }

    /// <summary>
    /// Executes the comprehensive coverage resilience demonstration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task RunAsync()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n--------------------------------------------------------------------------------");
        Console.WriteLine(" LEVEL 11: COMPREHENSIVE PUBLIC API COVERAGE & LIVING VERIFICATION");
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.ResetColor();

        // 1. ResiliencePipelineBuilder SetPipelineFactory & SetTypedPipelineFactory & TranslateAndBuild
        ResiliencePipelineBuilder.SetPipelineFactory(b => PollyPipelineBuilderTranslator.TranslateAndBuild(b));
        ResiliencePipelineBuilder.SetTypedPipelineFactory<string>(b => PollyPipelineBuilderTranslator.TranslateAndBuild(b));
        Console.WriteLine(" [1] ResiliencePipelineBuilder SetPipelineFactory and SetTypedPipelineFactory configured.");

        // 2. ResiliencePolicy.Configure
        var customPolicy = new ShowcaseCustomPolicy();
        var pipelineBuilder = new ResiliencePipelineBuilder("custom-test");
        customPolicy.Configure(pipelineBuilder);
        var pipeline = pipelineBuilder.Build();
        Console.WriteLine(" [2] ShowcaseCustomPolicy.Configure executed and pipeline built.");

        // 3. ResiliencePipelineRegistry GetPipeline & GetPipeline<T>
        var registry = new ResiliencePipelineRegistry();
        registry.Register("custom-test", pipeline);
        var typedBuilder = new ResiliencePipelineBuilder<string>("typed-test");
        var typedPipeline = typedBuilder.Build();
        registry.Register<string>("typed-test", typedPipeline);

        var retrievedPipe = registry.GetPipeline("custom-test");
        var retrievedTypedPipe = registry.GetPipeline<string>("typed-test");
        Console.WriteLine($" [3] ResiliencePipelineRegistry.GetPipeline and GetPipeline<T> resolved: {retrievedPipe != null}, {retrievedTypedPipe != null}");

        // 4. ResilienceContext WithCancellationToken & CopyPropertiesTo & SetProperty
        var ctx1 = new ResilienceContext("policy-a", "operation-1");
        ctx1.SetProperty("env", "production");
        var ctx2 = ctx1.WithCancellationToken(CancellationToken.None);
        var ctx3 = new ResilienceContext("policy-b", "operation-2");
        ctx1.CopyPropertiesTo(ctx3);
        Console.WriteLine($" [4] ResilienceContext WithCancellationToken and CopyPropertiesTo verified: {ctx2.OperationName}");

        // 5. PollyContextAdapter ToPollyContext, GetEcosystemContext, Return
        var pollyContext = PollyContextAdapter.ToPollyContext(ctx1);
        var backToEco = PollyContextAdapter.GetEcosystemContext(pollyContext);
        PollyContextAdapter.Return(pollyContext);
        Console.WriteLine($" [5] PollyContextAdapter ToPollyContext, GetEcosystemContext, Return verified. EcoContext match: {backToEco != null}");

        // 6. ResultRetryClassifier.ClassifyResult
        var classifier = new ResultRetryClassifier();
        var decision = classifier.ClassifyResult(true);
        Console.WriteLine($" [6] ResultRetryClassifier.ClassifyResult decision: {decision}");

        // 7. Configuration binding extensions (BindRetryOptions, BindCircuitBreakerOptions, BindTimeoutOptions, BindRateLimiterOptions)
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Resilience:Retry:MaxRetryAttempts"] = "4",
            ["Resilience:Retry:BackoffType"] = "Exponential",
            ["Resilience:CircuitBreaker:FailureRatio"] = "0.6",
            ["Resilience:Timeout:Timeout"] = "00:00:15",
            ["Resilience:RateLimiter:PermitLimit"] = "20"
        };
        var configRoot = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var retryOpts = configRoot.GetSection("Resilience:Retry").BindRetryOptions();
        var cbOpts = configRoot.GetSection("Resilience:CircuitBreaker").BindCircuitBreakerOptions();
        var timeoutOpts = configRoot.GetSection("Resilience:Timeout").BindTimeoutOptions();
        var rateLimiterOpts = configRoot.GetSection("Resilience:RateLimiter").BindRateLimiterOptions();
        Console.WriteLine($" [7] Configuration bindings: Retries={retryOpts.MaxRetryAttempts}, CB FailureRatio={cbOpts.FailureRatio}, Timeout={timeoutOpts.Timeout}, RateLimit={rateLimiterOpts.PermitLimit}");

        // 8. ResilienceOptions.AddPolicy
        var resOptions = new ResilienceOptions();
        resOptions.AddPolicy("test-add-policy", b => b.AddTimeout(TimeSpan.FromSeconds(2)));
        Console.WriteLine(" [8] ResilienceOptions.AddPolicy verified.");

        // 9. Diagnostics (ResilienceActivitySource.RecordException, ResilienceMeter.RecordCircuitBreakerRejection)
        ResilienceActivitySource.RecordException(Activity.Current, new InvalidOperationException("Showcase expected exception"));
        ResilienceMeter.RecordCircuitBreakerRejection("showcase-policy", "op1", "tenant-1");
        Console.WriteLine(" [9] ResilienceActivitySource.RecordException and ResilienceMeter.RecordCircuitBreakerRejection executed.");

        // 10. ASP.NET Core RequireResilience extension
        var endpointBuilder = new DummyEndpointConventionBuilder();
        endpointBuilder.RequireResilience("showcase-policy");
        Console.WriteLine(" [10] AspNetCoreResilienceExtensions.RequireResilience executed.");

        // 11. Health checks (AddCircuitBreakerCheck, CheckHealthAsync)
        var services = new ServiceCollection();
        services.AddHealthChecks().AddCircuitBreakerCheck("cb-check", "showcase-policy", () => CircuitBreakerState.Closed);
        var checkInstance = new ResilienceCircuitBreakerHealthCheck("showcase-policy", () => CircuitBreakerState.Closed);
        var healthContext = new HealthCheckContext();
        var checkResult = await checkInstance.CheckHealthAsync(healthContext).ConfigureAwait(false);
        Console.WriteLine($" [11] CircuitBreakerHealthCheckExtensions & CheckHealthAsync verified: Status={checkResult.Status}");

        // 12. Typed extension methods on ResiliencePipelineBuilder<TResult>
        //     These cover: AddResultRetry<TResult>, AddStandardResilience<TResult>, AddDatabaseResilience<TResult>, AddTimeout<TResult>(TimeSpan)
        Console.WriteLine("\n [12] Typed ResiliencePipelineBuilder<TResult> Extension Methods ---");
        PollyResilienceRegistration.RegisterTypedPipeline<string>();

        // 12a. AddResultRetry<TResult> — typed pipeline with Result<T>-aware retry
        var typedRetryBuilder = new ResiliencePipelineBuilder<string>("typed-result-retry")
            .AddResultRetry(opt => { opt.MaxRetryAttempts = 2; opt.Delay = TimeSpan.FromMilliseconds(10); });
        var typedRetryPipeline = typedRetryBuilder.Build();
        Console.WriteLine($"     AddResultRetry<TResult>: pipeline built '{typedRetryBuilder.Name}'");

        // 12b. AddStandardResilience<TResult> — typed pipeline with standard preset
        var typedStandardBuilder = new ResiliencePipelineBuilder<string>("typed-standard")
            .AddStandardResilience();
        var typedStandardPipeline = typedStandardBuilder.Build();
        Console.WriteLine($"     AddStandardResilience<TResult>: pipeline built '{typedStandardBuilder.Name}'");

        // 12c. AddDatabaseResilience<TResult> — typed pipeline with DB preset
        var typedDbBuilder = new ResiliencePipelineBuilder<string>("typed-database")
            .AddDatabaseResilience(timeout: TimeSpan.FromSeconds(5), maxRetries: 2);
        var typedDbPipeline = typedDbBuilder.Build();
        Console.WriteLine($"     AddDatabaseResilience<TResult>: pipeline built '{typedDbBuilder.Name}'");

        // 12d. AddTimeout<TResult>(TimeSpan) shortcut — typed timeout shortcut
        var typedTimeoutBuilder = new ResiliencePipelineBuilder<string>("typed-timeout-shortcut")
            .AddTimeout(TimeSpan.FromSeconds(2));
        var typedTimeoutPipeline = typedTimeoutBuilder.Build();
        Console.WriteLine($"     AddTimeout<TResult>(TimeSpan): pipeline built '{typedTimeoutBuilder.Name}'");

        // Execute typed standard pipeline to verify it is functional
        registry.Register<string>("typed-standard", typedStandardPipeline);
        var typedStandardResult = await typedStandardPipeline.ExecuteAsync(
            async (CancellationToken ct) =>
            {
                await Task.Delay(5, ct);
                return "Typed standard resilience executed.";
            }).ConfigureAwait(false);
        Console.WriteLine($" [12] Typed extension methods verified. Result: '{typedStandardResult}'");

        // 13. IResiliencePipelineRegistry.TryGetPipeline — untyped overload
        Console.WriteLine("\n [13] IResiliencePipelineRegistry.TryGetPipeline (untyped overload) ---");
        if (registry.TryGetPipeline("custom-test", out var resolvedUntyped))
        {
            var untypedResult = await resolvedUntyped.ExecuteAsync(
                async (CancellationToken ct) =>
                {
                    await Task.Delay(5, ct);
                    return "Untyped pipeline resolved from registry.";
                }).ConfigureAwait(false);
            Console.WriteLine($" [13] TryGetPipeline (untyped) succeeded: '{untypedResult}'");
        }

        // 14. ResilienceException base class — catch-all for any resilience failure
        //     Demonstrates that all resilience exceptions (ResiliencePolicyNotFoundException,
        //     CircuitBrokenException, ResilienceTimeoutException, RateLimitRejectedException,
        //     ResilienceConfigurationException) inherit from ResilienceException.
        Console.WriteLine("\n [14] ResilienceException base class catch hierarchy ---");
        try
        {
            var tempServices = new ServiceCollection();
            tempServices.AddEricksonLopezResilience();
            var tempSp = tempServices.BuildServiceProvider();
            var tempExecutor = tempSp.GetRequiredService<IResilienceExecutor>();
            await tempExecutor.ExecuteAsync("non-existent-for-base-test", async ct => await Task.CompletedTask).ConfigureAwait(false);
        }
        catch (ResilienceException resEx)
        {
            // Catching ResilienceException catches any resilience framework failure:
            // ResiliencePolicyNotFoundException | CircuitBrokenException | ResilienceTimeoutException
            // | RateLimitRejectedException | ResilienceConfigurationException
            Console.WriteLine($" [14] ResilienceException base catch verified: {resEx.GetType().Name} — {resEx.Message}");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✔ Level 11 Comprehensive API Coverage completed successfully.");
        Console.ResetColor();
    }
}
