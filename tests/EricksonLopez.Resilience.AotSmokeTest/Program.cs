// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Mediator.Extensions;
using EricksonLopez.Resilience.NativeAotTests;
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("==================================================");
Console.WriteLine(" EricksonLopez.Resilience NativeAOT Test Suite    ");
Console.WriteLine("==================================================");

int passedTests = 0;

void Assert([DoesNotReturnIf(false)] bool condition, string testName)
{
    if (!condition)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAIL] {testName}");
        Console.ResetColor();
        throw new InvalidOperationException($"Assertion failed for: {testName}");
    }

    passedTests++;
    Console.WriteLine($"[PASS] {testName}");
}

// ── 1. ResilienceContext & Properties ──────────────────────────────────────────
Console.WriteLine("\n--- 1. ResilienceContext Invariants & Immutability ---");

var context = ResilienceContext.Create("payment-policy")
    .WithOperationName("AuthorizePayment")
    .WithCorrelationId("corr-12345")
    .WithTenantId("tenant-abc")
    .WithAttemptNumber(2)
    .SetProperty("custom_flag", true);

Assert(context.PolicyName == "payment-policy", "PolicyName is set correctly");
Assert(context.OperationName == "AuthorizePayment", "OperationName is set correctly");
Assert(context.CorrelationId == "corr-12345", "CorrelationId is set correctly");
Assert(context.TenantId == "tenant-abc", "TenantId is set correctly");
Assert(context.AttemptNumber == 2, "AttemptNumber is set correctly");
Assert(context.TryGetProperty<bool>("custom_flag", out var flag) && flag, "Custom property stored and retrieved");

// ── 2. Error & Result Classification ──────────────────────────────────────────
Console.WriteLine("\n--- 2. Result & Error Retry Classification ---");

var classifier = ResultRetryClassifier.Instance;
var successResult = Result<string>.Success("ok");
Assert(classifier.ClassifyResult(successResult) == RetryabilityDecision.DoNotRetry, "Success result returns DoNotRetry");

var transientError = new Error("ERR_DB_HICCUP", "Database busy", ErrorType.Unavailable, ErrorSeverity.Error, ErrorRetryability.Transient);
var failureResult = Result<string>.Failure(transientError);
Assert(classifier.ClassifyResult(failureResult) == RetryabilityDecision.Retry, "Transient result failure returns Retry");

var permError = new Error("ERR_VALIDATION", "Invalid email", ErrorType.Validation, ErrorSeverity.Error, ErrorRetryability.Permanent);
Assert(classifier.ClassifyError(permError) == RetryabilityDecision.DoNotRetry, "Permanent validation error returns DoNotRetry");

var socketEx = new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.TimedOut);
Assert(TransientExceptionClassifier.IsTransient(socketEx), "SocketException is recognized as transient");

var timeoutEx = new ResilienceTimeoutException(TimeSpan.FromSeconds(5), "payment-policy");
Assert(TransientExceptionClassifier.IsTransient(timeoutEx), "ResilienceTimeoutException is transient");

// ── 3. Polly Pipeline Execution (Retry Strategy) ──────────────────────────────
Console.WriteLine("\n--- 3. Polly Pipeline Execution (Retry Strategy) ---");

var retryBuilder = new ResiliencePipelineBuilder("aot-retry-policy");
int retryCallbacks = 0;

retryBuilder.AddRetry(opt =>
{
    opt.MaxRetryAttempts = 3;
    opt.Delay = TimeSpan.FromMilliseconds(5);
    opt.BackoffType = BackoffType.Constant;
    opt.ShouldHandleException = ex => ex is IOException;
    opt.OnRetry = ctx =>
    {
        retryCallbacks++;
        return ValueTask.CompletedTask;
    };
});

var retryPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(retryBuilder);
int executionAttempts = 0;

var retryResult = await retryPipeline.ExecuteAsync<string>(async ct =>
{
    executionAttempts++;
    if (executionAttempts < 3)
    {
        throw new IOException("Transient network glitch");
    }
    return await ValueTask.FromResult("Recovered");
});

Assert(retryResult == "Recovered", "Pipeline returns recovered value after retries");
Assert(executionAttempts == 3, "Operation executed 3 times");
Assert(retryCallbacks == 2, "OnRetry callback invoked twice");

// ── 4. Polly Pipeline Execution (Circuit Breaker & Timeout) ───────────────────
Console.WriteLine("\n--- 4. Circuit Breaker & Timeout Strategies ---");

var cbBuilder = new ResiliencePipelineBuilder("aot-cb-policy");
cbBuilder.AddCircuitBreaker(opt =>
{
    opt.FailureRatio = 0.5;
    opt.MinimumThroughput = 2;
    opt.SamplingDuration = TimeSpan.FromSeconds(5);
    opt.BreakDuration = TimeSpan.FromMilliseconds(500);
    opt.ShouldHandleException = ex => ex is InvalidOperationException;
});

var cbPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(cbBuilder);

for (int i = 0; i < 2; i++)
{
    try
    {
        await cbPipeline.ExecuteAsync<string>(ct => throw new InvalidOperationException("Fail"));
    }
    catch (InvalidOperationException)
    {
        // Expected initial failures
    }
}

bool circuitTripped = false;
try
{
    await cbPipeline.ExecuteAsync<string>(ct => ValueTask.FromResult("ok"));
}
catch (CircuitBrokenException)
{
    circuitTripped = true;
}
Assert(circuitTripped, "Circuit breaker tripped and threw CircuitBrokenException");

// Timeout test
var timeoutBuilder = new ResiliencePipelineBuilder("aot-timeout-policy");
timeoutBuilder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(50) });
var timeoutPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(timeoutBuilder);

bool timeoutThrown = false;
try
{
    await timeoutPipeline.ExecuteAsync<string>(async ct =>
    {
        await Task.Delay(500, ct);
        return "late";
    });
}
catch (ResilienceTimeoutException)
{
    timeoutThrown = true;
}
Assert(timeoutThrown, "Timeout pipeline threw ResilienceTimeoutException");

// ── 5. Dependency Injection & Executor Resolution ─────────────────────────────
Console.WriteLine("\n--- 5. Dependency Injection & Resilience Executor ---");

var services = new ServiceCollection();
services.AddEricksonLopezResilience();
services.AddResiliencePolicy("order-service-policy", b =>
{
    b.AddRetry(opt =>
    {
        opt.MaxRetryAttempts = 2;
        opt.Delay = TimeSpan.FromMilliseconds(5);
    });
});

var serviceProvider = services.BuildServiceProvider();
var executor = serviceProvider.GetRequiredService<IResilienceExecutor>();
var registry = serviceProvider.GetRequiredService<IResiliencePipelineRegistry>();

Assert(registry.TryGetPipeline("order-service-policy", out var resolvedPipeline) && resolvedPipeline != null, "Policy registered and resolved from DI registry");

var execOutput = await executor.ExecuteAsync("order-service-policy", async ctx =>
{
    await Task.Yield();
    return 42;
});
Assert(execOutput == 42, "IResilienceExecutor executes operation successfully");

// ── 6. Mediator Behavior Integration ──────────────────────────────────────────
Console.WriteLine("\n--- 6. Mediator Resilience Behavior ---");

var mediatorServices = new ServiceCollection();
mediatorServices.AddEricksonLopezResilience();
mediatorServices.AddResiliencePolicy("query-policy", b =>
{
    b.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(5) });
});
mediatorServices.AddResiliencePipelineBehavior();

var mediatorProvider = mediatorServices.BuildServiceProvider();
var mediatorExecutor = mediatorProvider.GetRequiredService<IResilienceExecutor>();

var behavior = new ResiliencePipelineBehavior<SampleResilientQuery, Result<string>>(mediatorExecutor);
var sampleQuery = new SampleResilientQuery();
var mockNext = new AotMockNext();

var mediatorResponse = await behavior.Handle(sampleQuery, mockNext, CancellationToken.None);
Assert(mediatorResponse.IsSuccess && mediatorResponse.Value == "AotQuerySuccess", "ResiliencePipelineBehavior executed successfully");

// ── 7. OpenTelemetry Diagnostics Validation ──────────────────────────────────
Console.WriteLine("\n--- 7. OpenTelemetry Diagnostics ---");

ResilienceMeter.RecordExecution("test-policy", "Process", 15.2, true, "tenant-1");
ResilienceMeter.RecordRetry("test-policy", "Process", 1, "tenant-1");
ResilienceMeter.RecordCircuitStateChange("test-policy", CircuitBreakerState.Open, "tenant-1");
ResilienceMeter.RecordTimeout("test-policy", "Process", "tenant-1");
ResilienceMeter.RecordRateLimitRejection("test-policy", "Process", "tenant-1");

Assert(ResilienceMeter.MeterName == "EricksonLopez.Resilience", "Meter name is EricksonLopez.Resilience");
Assert(ResilienceActivitySource.SourceName == "EricksonLopez.Resilience", "ActivitySource name is EricksonLopez.Resilience");

// ── 8. Typed Pipelines & Fallback ─────────────────────────────────────────────
Console.WriteLine("\n--- 8. Typed Pipelines & Fallback ---");

var typedBuilder = new ResiliencePipelineBuilder<string>("aot-typed-fallback");
typedBuilder.AddFallback(new FallbackStrategyOptions<string>
{
    FallbackAction = _ => ValueTask.FromResult("AotFallbackValue"),
    ShouldHandleException = _ => true
});

var typedPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(typedBuilder);
var fallbackResult = await typedPipeline.ExecuteAsync(ct => throw new InvalidOperationException("AOT Error"));
Assert(fallbackResult == "AotFallbackValue", "Typed pipeline fallback executed in Native AOT");

Console.WriteLine("\n==================================================");
Console.WriteLine($" ALL {passedTests} NATIVE AOT SUITE TESTS PASSED SUCCESSFULLY! ");
Console.WriteLine("=== AOT Validator: OK ===");
Console.WriteLine("==================================================");

