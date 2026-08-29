// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.AspNetCore.Http;
using EricksonLopez.Resilience.AspNetCore.Metadata;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.OpenTelemetry.Extensions;
using EricksonLopez.Resilience.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Level 9 — Extensions: ASP.NET Core, HttpClient Delegating Handlers, and OpenTelemetry Observability.
/// </summary>
public static class Level9Extensions
{
    private const string WeatherPolicyName = "http-weather-service";
    private const string ForecastOperation = "FetchForecastAsync";
    private const string SampleTenantId = "TENANT-01";

    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Showcase sample base URIs")]
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 9] EXTENSIONS: ASP.NET CORE, HTTPCLIENT AND OPENTELEMETRY");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // 1. Policy with OpenTelemetry telemetry hooks (WithTelemetry)
        services.AddResiliencePolicy(WeatherPolicyName, builder =>
        {
            var timeoutOpt = new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(3) }.WithTelemetry();
            var retryOpt = new RetryStrategyOptions { MaxRetryAttempts = 2, Delay = TimeSpan.FromMilliseconds(50) }.WithTelemetry();
            var cbOpt = new CircuitBreakerStrategyOptions { FailureRatio = 0.5, SamplingDuration = TimeSpan.FromSeconds(10) }.WithTelemetry();

            builder
                .AddTimeout(timeoutOpt)
                .AddRetry(retryOpt)
                .AddCircuitBreaker(cbOpt);
        });

        // 2. Typed HttpClient configuration with resilient Delegating Handler
        services.AddHttpClient("WeatherApi", client =>
        {
            client.BaseAddress = new Uri("https://api.weather.example.com/");
        })
        .AddResiliencePolicy(WeatherPolicyName);

        // 3. Typed HttpClient configuration with Standard Resilience Handler preset
        services.AddHttpClient("PaymentGatewayApi", client =>
        {
            client.BaseAddress = new Uri("https://api.payments.example.com/");
        })
        .AddStandardResilienceHandler("http-standard-payments", builder =>
        {
            // Fine-tune timeout if necessary
            builder.AddTimeout(TimeSpan.FromSeconds(15));
        });

        _ = services.BuildServiceProvider();

        // 3. OpenTelemetry Instrumentation Demonstration (ActivitySource & Meters)
        Console.WriteLine("\n --- 1. OpenTelemetry Instrumentation ---");
        Console.WriteLine($"    ActivitySource Name: {ResilienceActivitySource.SourceName} (Version: {ResilienceActivitySource.SourceVersion})");
        Console.WriteLine($"    Meter Name:          {ResilienceMeter.MeterName} (Version: {ResilienceMeter.MeterVersion})");

        using (var activity = ResilienceActivitySource.StartExecutionActivity(WeatherPolicyName, ForecastOperation, tenantId: SampleTenantId, correlationId: "CORR-9988"))
        {
            Console.WriteLine($"    [Activity Created] Id: {activity?.Id ?? "LocalActivity"}, TraceId: {activity?.TraceId.ToString() ?? "N/A"}");

            // RecordExecution — operation duration and success/failure outcome
            ResilienceMeter.RecordExecution(WeatherPolicyName, ForecastOperation, 45.2, isSuccess: true, tenantId: SampleTenantId);
            Console.WriteLine("    [✓] RecordExecution emitted.");

            // RecordRetry — tracks individual retry attempts
            ResilienceMeter.RecordRetry(WeatherPolicyName, ForecastOperation, 1, tenantId: SampleTenantId);
            Console.WriteLine("    [✓] RecordRetry emitted.");

            // RecordCircuitStateChange — tracks circuit breaker transitions (Closed/Open/HalfOpen)
            ResilienceMeter.RecordCircuitStateChange(WeatherPolicyName, CircuitBreakerState.Open, tenantId: SampleTenantId);
            ResilienceMeter.RecordCircuitStateChange(WeatherPolicyName, CircuitBreakerState.HalfOpen, tenantId: SampleTenantId);
            ResilienceMeter.RecordCircuitStateChange(WeatherPolicyName, CircuitBreakerState.Closed, tenantId: SampleTenantId);
            Console.WriteLine("    [✓] RecordCircuitStateChange (Open, HalfOpen, Closed) emitted.");

            // RecordTimeout — tracks timeout rejections
            ResilienceMeter.RecordTimeout(WeatherPolicyName, ForecastOperation, tenantId: SampleTenantId);
            Console.WriteLine("    [✓] RecordTimeout emitted.");

            // RecordRateLimitRejection — tracks rate limiter rejections
            ResilienceMeter.RecordRateLimitRejection(WeatherPolicyName, ForecastOperation, tenantId: SampleTenantId);
            Console.WriteLine("    [✓] RecordRateLimitRejection emitted.");
        }

        // Summary: All three WithTelemetry() extension targets shown explicitly
        Console.WriteLine("\n --- 1b. WithTelemetry() Extension Targets (all three strategy types) ---");
        var retryWithTelemetry = new RetryStrategyOptions { MaxRetryAttempts = 2, Delay = TimeSpan.FromMilliseconds(50) }.WithTelemetry();
        var cbWithTelemetry = new CircuitBreakerStrategyOptions { MinimumThroughput = 5, SamplingDuration = TimeSpan.FromSeconds(10) }.WithTelemetry();
        var timeoutWithTelemetry = new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(3) }.WithTelemetry();

        Console.WriteLine($"    RetryStrategyOptions.WithTelemetry()         -> OnRetry hooked: {retryWithTelemetry.OnRetry != null}");
        Console.WriteLine($"    CircuitBreakerStrategyOptions.WithTelemetry() -> OnCircuitOpened hooked: {cbWithTelemetry.OnCircuitOpened != null}");
        Console.WriteLine($"    TimeoutStrategyOptions.WithTelemetry()        -> OnTimeout hooked: {timeoutWithTelemetry.OnTimeout != null}");

        // 4. Minimal API Endpoint Metadata Demonstration
        Console.WriteLine("\n --- 2. ASP.NET Core Endpoint Metadata (RequireResilience) ---");
        var metadata = new ResilienceEndpointMetadata(WeatherPolicyName);
        Console.WriteLine($"    ResilienceEndpointMetadata configured for endpoint: PolicyName='{metadata.PolicyName}'");

        Console.WriteLine("\n [✓] Level 9 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
        await Task.CompletedTask;
    }
}
