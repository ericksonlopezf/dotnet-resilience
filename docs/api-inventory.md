# Public API Inventory

This document provides a comprehensive inventory of public types, interfaces, options, and extension methods exported by the `EricksonLopez.Resilience` package ecosystem.

---

## 1. `EricksonLopez.Resilience.Abstractions`

### Core Interfaces & Pipelines
- `IResiliencePipeline`: Untyped execution pipeline supporting asynchronous delegates (`ExecuteAsync<TResult>(...)`).
- `IResiliencePipeline<TResult>`: Generic strongly typed execution pipeline for specific return models.
- `IResiliencePipelineBuilder`: Fluent builder contract for assembling multi-strategy pipelines.
- `IResiliencePipelineRegistry`: Registry contract for managing named and keyed resilience pipelines.
- `IResilienceExecutor`: High-level executor contract abstracting pipeline resolution and execution.
- `IResiliencePolicy`: Domain policy contract encapsulating pipeline definitions.
- `IResilientRequest`: Marker interface for Mediator requests to automatically bind to named resilience policies.

### Strategy Options Models
- `ResilienceStrategyOptions`: Abstract base class for strategy configuration.
- `RetryStrategyOptions`: Options configuring retry attempts, backoff formulas, delay caps, and error classifiers.
- `CircuitBreakerStrategyOptions`: Options configuring failure thresholds, break durations, sampling windows, and manual tripping.
- `RateLimiterStrategyOptions`: Options configuring permit limits, queue limits, replenishment windows, and algorithm types.
- `TimeoutStrategyOptions`: Options configuring operation timeout limits and callback notifications.
- `HedgingStrategyOptions`: Options configuring concurrent hedging attempts and fallback delays.
- `FallbackStrategyOptions`: Options configuring substitute fallback delegates.

### Enums & Contexts
- `BackoffType`: Enum specifying delay calculation (`Constant`, `Linear`, `Exponential`, `ExponentialWithJitter`).
- `CircuitBreakerState`: Enum indicating circuit status (`Closed`, `Open`, `HalfOpen`, `Isolated`).
- `RateLimiterType`: Enum defining limiter algorithm (`SlidingWindow`, `FixedWindow`, `Concurrency`, `TokenBucket`).
- `ResilienceContext`: Ambient and async execution context carrying execution identifiers and cancellation tokens.
- `RetryAttemptContext`: Contextual metadata passed to retry callbacks.
- `CircuitBreakerStateContext`: Contextual metadata passed to state change callbacks.

### Exceptions
- `ResilienceException`: Base exception for resilience strategy failures.
- `CircuitBrokenException`: Thrown when a request is rejected by an open or isolated circuit breaker.
- `RateLimitRejectedException`: Thrown when a request exceeds permitted rate limits.
- `ResilienceTimeoutException`: Thrown when an operation exceeds its configured execution timeout.
- `ResiliencePolicyNotFoundException`: Thrown when an unresolved named policy is requested from the registry.

---

## 2. `EricksonLopez.Resilience` (Core)

### Pipeline Implementations
- `ResiliencePipelineBuilder`: Production fluent builder constructing executable resilience pipelines.
- `ResiliencePipelineBuilder<TResult>`: Generic fluent builder for strongly typed pipelines.
- `ResiliencePipelineRegistry`: Thread-safe concurrent registry of named pipelines.
- `ResiliencePolicyRegistry`: Concurrent registry for named domain policies.
- `PassthroughResiliencePipeline`: Zero-overhead bypass pipeline for disabled resilience paths.

### Classifiers
- `ResultRetryClassifier`: Classifier evaluating functional `Result<T>` instances for transient errors.
- `TransientExceptionClassifier`: Classifier evaluating system, socket, and network exceptions for retryability.

---

## 3. `EricksonLopez.Resilience.Polly`

### Adapters & Translators
- `PollyPipelineBuilderTranslator`: Compiles `EricksonLopez.Resilience` pipeline definitions into optimized Polly v8 pipelines.
- `PollyResiliencePipeline`: Adapter executing underlying Polly v8 pipelines with zero allocation.
- `PollyResilienceExecutor`: Production executor resolving and invoking Polly-backed pipelines.
- `PollyContextAdapter`: Bi-directional bridge between `ResilienceContext` and `Polly.ResilienceContext`.

---

## 4. `EricksonLopez.Resilience.AspNetCore`

### HTTP Handlers & Middleware
- `ResilienceDelegatingHandler`: `DelegatingHandler` wrapping outbound `HttpClient` requests with resilience pipelines.
- `HttpClientResilienceExtensions`: Fluent extension methods for attaching resilience policies to `IHttpClientBuilder`.
- `AspNetCoreResilienceExtensions`: Endpoint routing helpers for attaching policy metadata.

---

## 5. `EricksonLopez.Resilience.Mediator`

### Pipeline Behaviors
- `ResiliencePipelineBehavior<TRequest, TResponse>`: Pipeline behavior for `EricksonLopez.Mediator` executing requests implementing `IResilientRequest` inside configured pipelines.

---

## 6. `EricksonLopez.Resilience.OpenTelemetry`

### Observability Instrumentation
- `ResilienceMeter`: OpenTelemetry metrics instrumenting execution counts, retry attempts, circuit trips, and execution latency.
- `ResilienceActivitySource`: Distributed tracing instrumentation producing spans for resilient execution boundaries.
