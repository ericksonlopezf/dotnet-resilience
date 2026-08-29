// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Level 6 — Error Handling: Deterministic Classification and Typed Exception Hierarchy.
/// </summary>
public static class Level6ErrorHandling
{
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 6] ERROR HANDLING: CLASSIFIERS AND EXCEPTION HIERARCHY");
        Console.WriteLine("================================================================================");

        var classifier = ResultRetryClassifier.Instance;

        // 1. Runtime Exception Classification
        Console.WriteLine("\n --- 1. Runtime Exception Classification ---");
        var socketEx = new SocketException(10054);
        var timeoutEx = new TimeoutException();
        var cancelEx = new OperationCanceledException();
        var argEx = new ArgumentNullException(null, "Value cannot be null.");
        var http503 = new HttpRequestException("Unavailable", null, System.Net.HttpStatusCode.ServiceUnavailable);
        var http404 = new HttpRequestException("NotFound", null, System.Net.HttpStatusCode.NotFound);

        Console.WriteLine($"    SocketException(10054)       -> {classifier.ClassifyException(socketEx)} (Transient: {TransientExceptionClassifier.IsTransient(socketEx)})");
        Console.WriteLine($"    TimeoutException             -> {classifier.ClassifyException(timeoutEx)} (Transient: {TransientExceptionClassifier.IsTransient(timeoutEx)})");
        Console.WriteLine($"    HttpRequestException (503)   -> {classifier.ClassifyException(http503)} (Transient: {TransientExceptionClassifier.IsTransient(http503)})");
        Console.WriteLine($"    HttpRequestException (404)   -> {classifier.ClassifyException(http404)} (Transient: {TransientExceptionClassifier.IsTransient(http404)})");
        Console.WriteLine($"    OperationCanceledException   -> {classifier.ClassifyException(cancelEx)} (Transient: {TransientExceptionClassifier.IsTransient(cancelEx)})");
        Console.WriteLine($"    ArgumentNullException        -> {classifier.ClassifyException(argEx)} (Transient: {TransientExceptionClassifier.IsTransient(argEx)})");

        // 2. Domain Error and Result<T> Objects Classification
        Console.WriteLine("\n --- 2. Domain Error Objects Classification (Result Pattern) ---");
        var errorInfra = Error.Infrastructure("Db.Unavailable", "Connection failed");
        var errorDomain = Error.Domain("User.Inactive", "User account suspended");
        var errorValidation = Error.Validation("Email.Invalid", "Invalid email format");
        var errorCustomTransient = new Error("Rate.Limit", "Too many requests", ErrorType.Failure, ErrorSeverity.Error, ErrorRetryability.Transient);

        Console.WriteLine($"    Error.Infrastructure         -> {classifier.ClassifyError(errorInfra)}");
        Console.WriteLine($"    Error.Domain                 -> {classifier.ClassifyError(errorDomain)}");
        Console.WriteLine($"    Error.Validation             -> {classifier.ClassifyError(errorValidation)}");
        Console.WriteLine($"    Error with Retryability.Transient -> {classifier.ClassifyError(errorCustomTransient)}");

        // 3. Typed Framework Exception Interception Demonstration
        Console.WriteLine("\n --- 3. Framework Typed Exception Hierarchy ---");
        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        try
        {
            await executor.ExecuteAsync("non-existent-policy", async ct => await Task.CompletedTask);
        }
        catch (ResiliencePolicyNotFoundException ex)
        {
            Console.WriteLine($"    [✓] Caught ResiliencePolicyNotFoundException: {ex.Message} (Policy: {ex.PolicyName})");
        }

        // 4. RateLimitRejectedException and ResilienceConfigurationException
        Console.WriteLine("\n --- 4. RateLimitRejectedException and ResilienceConfigurationException ---");

        // Trigger RateLimitRejectedException by setting a micro-quota
        services.AddResiliencePolicy("l6-rate-reject", b =>
        {
            b.AddRateLimiter(opt =>
            {
                opt.PermitLimit = 1;
                opt.QueueLimit = 0;
                opt.Window = TimeSpan.FromMinutes(1);
            });
        });
        var sp2 = services.BuildServiceProvider();
        var executor2 = sp2.GetRequiredService<IResilienceExecutor>();
        await executor2.ExecuteAsync("l6-rate-reject", async ct => await Task.CompletedTask); // exhaust the 1 permit
        try
        {
            await executor2.ExecuteAsync("l6-rate-reject", async ct => await Task.CompletedTask);
        }
        catch (RateLimitRejectedException rlEx)
        {
            Console.WriteLine($"    [\u2713] Caught RateLimitRejectedException: PolicyName='{rlEx.PolicyName}', RetryAfter={rlEx.RetryAfter?.TotalSeconds:F1}s");
        }

        // ResilienceConfigurationException — triggered by invalid options
        try
        {
            var invalidBuilder = new EricksonLopez.Resilience.Builder.ResiliencePipelineBuilder("invalid");
            invalidBuilder.AddRetry(opt => { opt.MaxRetryAttempts = -1; }); // negative = invalid
        }
        catch (ResilienceConfigurationException cfgEx)
        {
            Console.WriteLine($"    [\u2713] Caught ResilienceConfigurationException: {cfgEx.Message}");
        }

        // CircuitBreakerState.Isolated — documented via enum reference (manual isolation is Polly-internal)
        Console.WriteLine($"    CircuitBreakerState values: Closed={CircuitBreakerState.Closed}, Open={CircuitBreakerState.Open}, HalfOpen={CircuitBreakerState.HalfOpen}, Isolated={CircuitBreakerState.Isolated}");

        // 5. ResilienceTimeoutException — triggered when operation exceeds configured timeout
        Console.WriteLine("\n --- 5. ResilienceTimeoutException (Pipeline Timeout Exceeded) ---");
        services.AddResiliencePolicy("l6-timeout-demo", b =>
        {
            b.AddTimeout(opt => { opt.Timeout = TimeSpan.FromMilliseconds(50); });
        });
        var sp3 = services.BuildServiceProvider();
        var executor3 = sp3.GetRequiredService<IResilienceExecutor>();
        try
        {
            await executor3.ExecuteAsync("l6-timeout-demo", async (CancellationToken ct) =>
            {
                await Task.Delay(500, ct); // Will be cancelled by the 50ms pipeline timeout
            });
        }
        catch (ResilienceTimeoutException toEx)
        {
            Console.WriteLine($"    [✓] Caught ResilienceTimeoutException: PolicyName='{toEx.PolicyName}', Timeout={toEx.Timeout.TotalMilliseconds}ms");
        }
        catch (OperationCanceledException)
        {
            // Polly surfaces the timeout as OperationCanceledException when CancellationToken is cancelled
            Console.WriteLine("    [✓] Timeout-induced OperationCanceledException caught (policy 'l6-timeout-demo', timeout=50ms).");
        }

        Console.WriteLine("\n [✓] Level 6 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }
}
