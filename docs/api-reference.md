# API Reference: EricksonLopez.Resilience

This document provides complete Microsoft Learn-style documentation for the public API of the `EricksonLopez.Resilience` library ecosystem across .NET 8, .NET 9, and .NET 10.

---

## Table of Contents
1. [IResilienceExecutor](#iresilienceexecutor)
2. [IResiliencePipeline & IResiliencePipeline\<TResult\>](#iresiliencepipeline--iresiliencepipelinetresult)
3. [IResiliencePipelineBuilder & ResiliencePipelineBuilder\<TResult\>](#iresiliencepipelinebuilder--resiliencepipelinebuildertresult)
4. [IResiliencePipelineRegistry](#iresiliencepipelineregistry)
5. [ResilienceContext](#resiliencecontext)
6. [Classification & Invariants](#classification--invariants)
7. [Strategy Options](#strategy-options)
8. [Exceptions Hierarchy](#exceptions-hierarchy)
9. [Dependency Injection & Configuration](#dependency-injection--configuration)
10. [Mediator Integration](#mediator-integration)
11. [Observability & OpenTelemetry](#observability--opentelemetry)
12. [ASP.NET Core & HttpClient](#aspnet-core--httpclient)

---

## IResilienceExecutor
Namespace: `EricksonLopez.Resilience`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

The primary execution facade injected into application services to dispatch operations under named resilience policies.

### Methods

#### `ExecuteAsync<TResult>(string policyName, Func<ResilienceContext, ValueTask<TResult>> operation, ResilienceContext context, CancellationToken cancellationToken = default)`
Executes an asynchronous delegate returning a value under the specified resilience policy.
- **Parameters**:
  - `policyName` (`string`): The registered policy name.
  - `operation` (`Func<ResilienceContext, ValueTask<TResult>>`): The delegate to execute.
  - `context` (`ResilienceContext`): The contextual execution metadata.
  - `cancellationToken` (`CancellationToken`): The cancellation token.
- **Returns**: `ValueTask<TResult>` yielding the result produced by the delegate.
- **Exceptions**: `ResiliencePolicyNotFoundException`, `CircuitBrokenException`, `RateLimitRejectedException`, `ResilienceTimeoutException`.

#### `ExecuteAsync<TResult>(string policyName, Func<CancellationToken, ValueTask<TResult>> operation, CancellationToken cancellationToken = default)`
Executes an asynchronous delegate accepting only a cancellation token.

#### `ExecuteAsync(string policyName, Func<ResilienceContext, ValueTask> operation, ResilienceContext context, CancellationToken cancellationToken = default)`
Executes an asynchronous void operation under the specified policy.

#### `ExecuteAsync(string policyName, Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default)`
Executes an asynchronous void operation accepting a cancellation token.

---

## IResiliencePipeline & IResiliencePipeline\<TResult\>
Namespace: `EricksonLopez.Resilience`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

Represents an individual compiled resilience pipeline instance.

### `IResiliencePipeline` Methods
- `ExecuteAsync(Func<ResilienceContext, ValueTask> callback, ResilienceContext context)`: Executes void action with context.
- `ExecuteAsync<TResult>(Func<ResilienceContext, ValueTask<TResult>> callback, ResilienceContext context)`: Executes typed action with context.
- `ExecuteAsync(Func<CancellationToken, ValueTask> callback, CancellationToken cancellationToken = default)`: Executes void action with cancellation token.
- `ExecuteAsync<TResult>(Func<CancellationToken, ValueTask<TResult>> callback, CancellationToken cancellationToken = default)`: Executes typed action with cancellation token.

### `IResiliencePipeline<TResult>` Methods
- `ExecuteAsync(Func<ResilienceContext, ValueTask<TResult>> callback, ResilienceContext context)`: Executes strongly-typed callback with context.
- `ExecuteAsync(Func<CancellationToken, ValueTask<TResult>> callback, CancellationToken cancellationToken = default)`: Executes strongly-typed callback with cancellation token.

---

## IResiliencePipelineBuilder & ResiliencePipelineBuilder\<TResult\>
Namespaces: `EricksonLopez.Resilience` *(interface)*, `EricksonLopez.Resilience.Builder` *(concrete implementation)*  
Assemblies: `EricksonLopez.Resilience.Abstractions.dll` *(IResiliencePipelineBuilder)*, `EricksonLopez.Resilience.dll` *(ResiliencePipelineBuilder, ResiliencePipelineBuilder\<TResult\>)*

Fluent builders used to configure and compose resilience strategies into immutable pipelines.

### Builder Strategy Methods
- `AddRetry(RetryStrategyOptions options)` / `AddRetry(Action<RetryStrategyOptions> configure)`: Adds retry strategy.
- `AddCircuitBreaker(CircuitBreakerStrategyOptions options)` / `AddCircuitBreaker(Action<CircuitBreakerStrategyOptions> configure)`: Adds circuit breaker.
- `AddTimeout(TimeoutStrategyOptions options)` / `AddTimeout(TimeSpan timeout)`: Adds execution deadline timeout.
- `AddRateLimiter(RateLimiterStrategyOptions options)` / `AddRateLimiter(Action<RateLimiterStrategyOptions> configure)`: Adds rate limiter.
- `AddFallback(FallbackStrategyOptions<TResult> options)` *(Typed builder)*: Adds fallback value or generator.
- `AddHedging(HedgingStrategyOptions options)` *(Untyped builder)* / `AddHedging(HedgingStrategyOptions<TResult> options)` *(Typed builder)*: Adds speculative hedging.
- `AddResultRetry(Action<RetryStrategyOptions>? configure = null, IResultRetryClassifier? classifier = null)`: Extension adding result-aware retry.
- `AddStandardResilience()`: Preset adding **Timeout (30s)** + **Retry (MaxRetryAttempts=3, Delay=1s, BackoffType=ExponentialWithJitter)** + **Circuit Breaker (FailureRatio=0.5, MinimumThroughput=20, SamplingDuration=30s, BreakDuration=10s)**. Also available as `AddStandardResilience<TResult>()` for typed pipelines.
- `AddDatabaseResilience(TimeSpan? timeout = null, int maxRetries = 3)`: Preset optimized for database connections. Adds **Timeout (default 15s, configurable)** + **Retry (configurable maxRetries, Delay=200ms, BackoffType=ExponentialWithJitter, MaxDelay=2s)** using `ResultRetryClassifier` for transient DB exception detection.

---

## IResiliencePipelineRegistry
Namespace: `EricksonLopez.Resilience`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

Read-only registry contract for resolving compiled named `IResiliencePipeline` and `IResiliencePipeline<TResult>` instances.

### Methods
- `GetPipeline(string policyName)`: Resolves untyped pipeline or throws `ResiliencePolicyNotFoundException`.
- `GetPipeline<TResult>(string policyName)`: Resolves typed pipeline or throws `ResiliencePolicyNotFoundException`.
- `TryGetPipeline(string policyName, out IResiliencePipeline? pipeline)`: Non-throwing untyped resolution.
- `TryGetPipeline<TResult>(string policyName, out IResiliencePipeline<TResult>? pipeline)`: Non-throwing typed resolution.

### ResiliencePipelineRegistry (Concrete Implementation)
Namespace: `EricksonLopez.Resilience.Registry`  
Assembly: `EricksonLopez.Resilience.dll`

Thread-safe, lock-free in-memory implementation of `IResiliencePipelineRegistry` backed by `ConcurrentDictionary`. Exposes mutation methods:
- `Register(string policyName, IResiliencePipeline pipeline)`: Registers a compiled untyped pipeline.
- `Register<TResult>(string policyName, IResiliencePipeline<TResult> pipeline)`: Registers a compiled typed pipeline.

---

## ResilienceContext
Namespace: `EricksonLopez.Resilience`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

Immutable-friendly execution context carrying operation metadata across retry attempts without ambient state.

### Properties & Methods
- `PolicyName` (`string`): The logical policy name.
- `OperationName` (`string`): The operation name (for logging and tracing).
- `CorrelationId` (`string?`): The distributed correlation ID.
- `TenantId` (`string?`): The tenant identifier for multi-tenant isolation.
- `AttemptNumber` (`int`): The 1-based attempt index.
- `Properties` (`IReadOnlyDictionary<string, object?>`): Custom context property dictionary.
- `WithOperationName(string)`: Returns a new context instance with the updated operation name.
- `WithCorrelationId(string)`: Returns a new context instance with the updated correlation ID.
- `WithTenantId(string)`: Returns a new context instance with the updated tenant ID.
- `WithAttemptNumber(int)`: Returns a new context instance with the updated attempt number.
- `SetProperty(string key, object? value)`: Sets a custom key-value property.
- `TryGetProperty<T>(string key, out T? value)`: Retrieves a strongly-typed property.
- `Create(string policyName, CancellationToken cancellationToken = default)`: Factory helper method.

---

## Classification & Invariants
Namespace: `EricksonLopez.Resilience.Classification`  
Assemblies: `EricksonLopez.Resilience.Abstractions.dll`, `EricksonLopez.Resilience.dll`

- **`IResultRetryClassifier`**: Evaluates `ClassifyResult<T>(T result)` and `ClassifyException(Exception exception)`.
- **`IErrorClassifier`**: Evaluates `ClassifyError(object? errorInstance)` returning `RetryabilityDecision`.
- **`RetryabilityDecision`**: `enum` (byte-backed) with three members: `Undetermined = 0`, `Retry = 1`, `DoNotRetry = 2`. Used by classifiers to communicate retry intent without throwing exceptions.
- **`ResultRetryClassifier`**: Default classifier inspecting `Result<T>`, `ErrorType`, and `ErrorRetryability`.
- **`TransientExceptionClassifier`**: Evaluates transient network, socket, and timeout exceptions.

---

## Strategy Options
Namespace: `EricksonLopez.Resilience.Options`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

- **`RetryStrategyOptions`**: `MaxRetryAttempts`, `Delay`, `MaxDelay`, `BackoffType` (`Constant`, `Linear`, `Exponential`, `ExponentialWithJitter`), `ShouldHandleException`, `ShouldHandleResult`, `OnRetry`.
- **`CircuitBreakerStrategyOptions`**: `FailureRatio`, `MinimumThroughput`, `SamplingDuration`, `BreakDuration`, `ShouldHandleException`, `ShouldHandleResult`, `OnCircuitOpened`, `OnCircuitHalfOpened`, `OnCircuitClosed`.
- **`TimeoutStrategyOptions`**: `Timeout`, `OnTimeout`.
- **`RateLimiterStrategyOptions`**: `PermitLimit`, `QueueLimit`, `Window`, `LimiterType` (`SlidingWindow`, `FixedWindow`, `TokenBucket`, `Concurrency`), `CustomRateLimiter`, `OnRejected`.
- **`FallbackStrategyOptions<TResult>`**: `FallbackAction`, `ShouldHandleException`, `ShouldHandleResult`, `OnFallback`.
- **`HedgingStrategyOptions`** *(Untyped)*: `MaxHedgedAttempts`, `Delay`, `ShouldHandleException`, `ShouldHandleResult`.
- **`HedgingStrategyOptions<TResult>`** *(Typed)*: `MaxHedgedAttempts`, `Delay`, `ShouldHandleException`, `ShouldHandleResult`, `OnHedging`, `HedgedActionGenerator`.

---

## Exceptions Hierarchy
Namespace: `EricksonLopez.Resilience.Exceptions`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

- **`ResilienceException`**: Root base exception for all resilience errors.
- **`CircuitBrokenException`**: Thrown when execution is rejected due to Open circuit breaker (`PolicyName`, `RetryAfter`).
- **`RateLimitRejectedException`**: Thrown when execution is throttled (`PolicyName`, `RetryAfter`).
- **`ResilienceTimeoutException`**: Thrown when execution exceeds deadline (`Timeout`, `PolicyName`).
- **`ResiliencePolicyNotFoundException`**: Thrown when policy name is missing from registry (`PolicyName`).
- **`ResilienceConfigurationException`**: Thrown on invalid strategy configuration options.

---

## Dependency Injection & Configuration
Namespace: `EricksonLopez.Resilience.DependencyInjection`  
Assembly: `EricksonLopez.Resilience.DependencyInjection.dll`

- `services.AddEricksonLopezResilience(Action<ResilienceOptions>? configure = null)`: Registers core registries and executors.
- `services.AddResiliencePolicy<TPolicy>()`: Registers a strongly-typed `ResiliencePolicy` subclass.
- `services.AddResiliencePolicy(string name, Action<IResiliencePipelineBuilder> configure)`: Registers an inline policy.
- `services.AddResiliencePolicy(string name, Action<IResiliencePipelineBuilder, IServiceProvider> configure)`: Registers an inline policy with access to `IServiceProvider` for resolving dependencies dynamically.
- `services.AddResiliencePolicyFromConfiguration(string policyName, IConfigurationSection section)`: Registers an inline policy by reading `Retry`, `CircuitBreaker`, `Timeout`, and `RateLimiter` sub-sections from the provided `IConfigurationSection`. Uses `BindRetryOptions`, `BindCircuitBreakerOptions`, `BindTimeoutOptions`, `BindRateLimiterOptions` internally.
- `ResilienceConfigurationExtensions`: AOT-safe explicit binders (`BindRetryOptions`, `BindCircuitBreakerOptions`, `BindTimeoutOptions`, `BindRateLimiterOptions`) for mapping `IConfigurationSection` to strategy options. Scalar fields use reflection-free parsers (`int.TryParse`, `double.TryParse`, `TimeSpan.TryParse`); enum fields (`BackoffType`, `RateLimiterType`) use `Enum.TryParse<T>` with compile-time-known generic parameters (AOT-compatible via .NET 8+ generic specialization).

### ResiliencePolicy (Abstract Base Class)
Namespace: `EricksonLopez.Resilience.Policies`  
Assembly: `EricksonLopez.Resilience.Abstractions.dll`

Abstract base class for declaring strongly-typed, named resilience policies. Subclass and override `Name` and `Configure` to encapsulate policy configuration as a first-class domain object.

```csharp
using EricksonLopez.Resilience;
using EricksonLopez.Resilience.Policies;
using Microsoft.Extensions.DependencyInjection;

public sealed class MyPolicy : ResiliencePolicy
{
    public override string Name => "my-policy";
    public override void Configure(IResiliencePipelineBuilder builder)
    {
        builder.AddRetry(opt => { opt.MaxRetryAttempts = 3; })
               .AddTimeout(TimeSpan.FromSeconds(10));
    }
}

// Registration in DI:
services.AddResiliencePolicy<MyPolicy>();
```

### PassthroughResiliencePipeline
Namespace: `EricksonLopez.Resilience`  
Assembly: `EricksonLopez.Resilience.dll`

A no-op `IResiliencePipeline` implementation that executes the operation directly without applying any resilience strategies. Intended for use in unit tests and integration test scenarios where resilience behavior should be bypassed.

```csharp
// Register in tests:
services.AddSingleton<IResiliencePipeline>(PassthroughResiliencePipeline.Instance);
```



---

## Mediator Integration
Namespaces: `EricksonLopez.Resilience.Mediator`, `EricksonLopez.Resilience.Mediator.Contracts`, `EricksonLopez.Resilience.Mediator.Extensions`  
Assembly: `EricksonLopez.Resilience.Mediator.dll`

- **`IResilientRequest`**: Interface marking commands/queries for automatic resilience dispatch (`string ResiliencePolicy { get; }`).
- **`ResiliencePipelineBehavior<TRequest, TResponse>`**: Sealed-class pipeline behavior that consumes struct-based continuation delegates (`where TNext : struct, INext<TResponse>`) to minimize continuation allocations.
- **`services.AddResiliencePipelineBehavior()`**: DI registration extension.

---

## Observability & OpenTelemetry
Namespaces: `EricksonLopez.Resilience.OpenTelemetry`, `EricksonLopez.Resilience.OpenTelemetry.Extensions`  
Assembly: `EricksonLopez.Resilience.OpenTelemetry.dll`

- **`ResilienceMeter`**: Emits `resilience.execution.duration`, `resilience.retry.attempts`, `resilience.circuit_breaker.state_changes`, `resilience.timeout.rejections`, `resilience.rate_limiter.rejections`.
- **`options.WithTelemetry()`**: Extension methods (`OpenTelemetryResilienceExtensions`) attaching automated metrics and tracing callbacks to `RetryStrategyOptions`, `CircuitBreakerStrategyOptions`, and `TimeoutStrategyOptions`.
- **OpenTelemetry Registration**: Configured via standard .NET `services.AddOpenTelemetry().WithMetrics(m => m.AddMeter("EricksonLopez.Resilience")).WithTracing(t => t.AddSource("EricksonLopez.Resilience"))`.

---

## ASP.NET Core & HttpClient
Namespaces: `EricksonLopez.Resilience.AspNetCore`, `EricksonLopez.Resilience.AspNetCore.Extensions`, `EricksonLopez.Resilience.AspNetCore.Metadata`  
Assembly: `EricksonLopez.Resilience.AspNetCore.dll`

- `endpoint.RequireResilience<TBuilder>(string policyName) where TBuilder : IEndpointConventionBuilder`: Attaches `ResilienceEndpointMetadata` to Minimal API and controller endpoints. Type parameter is inferred by the compiler.
- `httpClientBuilder.AddResiliencePolicy(string policyName)`: Attaches `ResilienceDelegatingHandler` executing the named policy.
- `httpClientBuilder.AddStandardResilienceHandler()`: Attaches out-of-the-box standard resilience handler preset.
