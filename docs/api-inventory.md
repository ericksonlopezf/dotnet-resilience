# Public API Inventory: EricksonLopez.Resilience

This document provides the exhaustive, rigorous, and official inventory of all public elements (classes, interfaces, records, enums, structs, attributes, builders, extension methods, options, and services) exported by the libraries classified across the **Foundation**, **Core**, **Integrations**, and **Infrastructure** layers of the ecosystem.

---

## 1. EricksonLopez.Resilience.Abstractions (Foundation — L0)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `IResiliencePipeline` | `EricksonLopez.Resilience` | Defines the untyped execution contract for fault-tolerant asynchronous delegates. | BCL, `ResilienceContext` | Executing heterogeneous operations returning arbitrary types or void. | Basic | Yes (`Level01RetryAndCircuitBreakers`, `Cookbook Recipe 1`) |
| `IResiliencePipeline<TResult>` | `EricksonLopez.Resilience` | Defines the strongly-typed pipeline for operations with a specific return type. | BCL, `ResilienceContext` | Pipelines requiring typed strategies (Fallback, Speculative Parallel Hedging). | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 8, 10, 19`) |
| `IResiliencePipelineBuilder` | `EricksonLopez.Resilience` | Fluent contract for composing resilience strategies into an executable pipeline. | `ResilienceStrategyOptions` | Assembling and configuring resilience policies at startup or runtime. | Intermediate | Yes (`Level02RateLimitingAndHedging`, `Level08CustomPoliciesTypedPipelines`) |
| `IResiliencePipelineRegistry` | `EricksonLopez.Resilience` | In-memory registry contract for storing and resolving compiled pipelines by policy name. | `IResiliencePipeline`, `IResiliencePipeline<TResult>` | Centralized and thread-safe pipeline resolution in DI containers. | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`, `Level11ComprehensiveCoverage`) |
| `IResilienceExecutor` | `EricksonLopez.Resilience` | Primary decoupled execution facade for application services and domain handlers. | `ResilienceContext` | Injected into domain/application layers to execute policies without coupling to Polly. | Basic | Yes (`Level00Introduction` through `Level10EnterpriseArchitecture`) |
| `ResilienceContext` | `EricksonLopez.Resilience` | Immutable context carrying execution metadata (`OperationName`, `CorrelationId`, `TenantId`, `AttemptNumber`, `Properties`). | BCL | Traceability propagation, multi-tenancy context, and metadata transfer across retry callbacks. | Intermediate | Yes (`Level05ProcessingMultitenancy`, `Cookbook Recipe 14`) |
| `IErrorClassifier` | `EricksonLopez.Resilience.Classification` | Evaluates domain error objects to determine deterministic retryability. | `RetryabilityDecision` | Error classification in domain-driven retry pipelines. | Intermediate | Yes (`Level06ErrorClassification`, `Level08CustomPoliciesTypedPipelines`) |
| `IResultRetryClassifier` | `EricksonLopez.Resilience.Classification` | Evaluates functional results and runtime exceptions for retry eligibility. | `RetryabilityDecision` | Retry strategies aware of Result Pattern and network/database exceptions. | Intermediate | Yes (`Level06ErrorClassification`, `Level08CustomPoliciesTypedPipelines`) |
| `RetryabilityDecision` | `EricksonLopez.Resilience.Classification` | Enumeration indicating retry determination (`Undetermined`, `Retry`, `DoNotRetry`). | BCL | Deterministic decision outcome returned by classifiers. | Basic | Yes (`Level06ErrorClassification`) |
| `ResilienceException` | `EricksonLopez.Resilience.Exceptions` | Base exception class for all resilience framework faults and anomalies. | BCL (`Exception`) | Unified catching of resilience and policy execution failures. | Basic | Yes (`Level06ErrorClassification`) |
| `CircuitBrokenException` | `EricksonLopez.Resilience.Exceptions` | Thrown when an operation is rejected because the Circuit Breaker is in `Open` state. | `ResilienceException` | Graceful degradation and fast-fail mitigation during persistent downstream outages. | Intermediate | Yes (`Level01RetryAndCircuitBreakers`, `Cookbook Recipe 2`) |
| `RateLimitRejectedException` | `EricksonLopez.Resilience.Exceptions` | Thrown when an operation exceeds configured concurrency or rate limits. | `ResilienceException` | Ingress overload control and mapping to HTTP 429 (`RetryAfter`). | Intermediate | Yes (`Level02RateLimitingAndHedging`, `Cookbook Recipe 11`) |
| `ResilienceConfigurationException` | `EricksonLopez.Resilience.Exceptions` | Thrown when a strategy or pipeline is configured with invalid or contradictory parameters. | `ResilienceException` | Early fail-fast validation during host startup. | Basic | Yes (`Level06ErrorClassification`) |
| `ResiliencePolicyNotFoundException` | `EricksonLopez.Resilience.Exceptions` | Thrown when a requested policy name is not registered in the pipeline registry. | `ResilienceException` | Fast diagnosis of missing policy registrations during execution resolution. | Basic | Yes (`Level06ErrorClassification`) |
| `ResilienceTimeoutException` | `EricksonLopez.Resilience.Exceptions` | Thrown when an operation execution exceeds the maximum allocated pipeline deadline. | `ResilienceException` | SLA enforcement and cleanup of hung or abandoned calls. | Basic | Yes (`Level06ErrorClassification`) |
| `BackoffType` | `EricksonLopez.Resilience.Options` | Enumeration defining the backoff interval algorithm (`Constant`, `Linear`, `Exponential`, `ExponentialWithJitter`). | BCL | Configuring the delay curve between retry attempts. | Basic | Yes (`Level01RetryAndCircuitBreakers`, `Cookbook Recipe 12`) |
| `CircuitBreakerState` | `EricksonLopez.Resilience.Options` | Enumeration representing circuit state (`Closed`, `Open`, `HalfOpen`, `Isolated`). | BCL | Observability, telemetry gauges, and ASP.NET Core health checks. | Basic | Yes (`Level09HttpAspnetcoreOpentelemetry`, `Level11ComprehensiveCoverage`) |
| `CircuitBreakerStateContext` | `EricksonLopez.Resilience.Options` | Contextual metadata provided when a circuit breaker state transition occurs. | `ResilienceContext`, `CircuitBreakerState` | Telemetry callbacks and structured logging on circuit state changes. | Intermediate | Yes (`Level01RetryAndCircuitBreakers`, `Level09HttpAspnetcoreOpentelemetry`) |
| `CircuitBreakerStrategyOptions` | `EricksonLopez.Resilience.Options` | Configuration options for Circuit Breaker (`FailureRatio`, `MinimumThroughput`, `BreakDuration`, etc.). | `ResilienceStrategyOptions` | Protecting downstream services against cascading collapse. | Intermediate | Yes (`Level01RetryAndCircuitBreakers`, `Cookbook Recipe 2`) |
| `FallbackContext` | `EricksonLopez.Resilience.Options` | Contextual metadata passed to fallback actions and callbacks upon activation. | `ResilienceContext`, `Exception` | Generating context-aware degraded responses or contingency data. | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 15`) |
| `FallbackStrategyOptions<TResult>` | `EricksonLopez.Resilience.Options` | Configuration options defining substitute actions, predicates, and callbacks for typed pipelines. | `ResilienceStrategyOptions` | Returning fallback cached data or default values on failure. | Advanced | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 8, 15`) |
| `HedgingContext` | `EricksonLopez.Resilience.Options` | Contextual metadata provided when starting a parallel speculative attempt. | `ResilienceContext` | Telemetry and replica routing during speculative hedging. | Advanced | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 19`) |
| `HedgingStrategyOptions` | `EricksonLopez.Resilience.Options` | Configuration options for speculative retries in untyped pipelines (sequential fallback). | `ResilienceStrategyOptions` | Quick sequential retries without delay for idempotent operations. | Intermediate | Yes (`Level02RateLimitingAndHedging`, `Cookbook Recipe 16`) |
| `HedgingStrategyOptions<TResult>` | `EricksonLopez.Resilience.Options` | Configuration options for typed parallel speculative hedging with `HedgedActionGenerator`. | `ResilienceStrategyOptions` | Mitigating tail latency spikes in active-active idempotent queries. | Advanced | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 10, 19`) |
| `RateLimiterContext` | `EricksonLopez.Resilience.Options` | Contextual metadata delivered to the rate limiter rejection callback. | `ResilienceContext` | Handling rejections and computing Retry-After HTTP headers. | Basic | Yes (`Level02RateLimitingAndHedging`, `Level05ProcessingMultitenancy`) |
| `RateLimiterStrategyOptions` | `EricksonLopez.Resilience.Options` | Rate limiter configuration options (`PermitLimit`, `QueueLimit`, `Window`, `LimiterType`, `CustomRateLimiter`). | `ResilienceStrategyOptions` | Ingress concurrency control and thundering herd protection. | Intermediate | Yes (`Level02RateLimitingAndHedging`, `Level05ProcessingMultitenancy`) |
| `RateLimiterType` | `EricksonLopez.Resilience.Options` | Enumeration of rate limiter algorithms (`SlidingWindow`, `FixedWindow`, `TokenBucket`, `Concurrency`). | BCL | Selecting the rate limiting algorithm based on traffic profile. | Basic | Yes (`Level02RateLimitingAndHedging`, `Cookbook Recipe 13`) |
| `ResilienceStrategyOptions` | `EricksonLopez.Resilience.Options` | Abstract base class for common strategy options (`Name`, `Order`). | BCL | Polymorphic base for all strategy configuration models. | Basic | Yes (`Level01RetryAndCircuitBreakers`, `Cookbook Recipe 16`) |
| `RetryAttemptContext` | `EricksonLopez.Resilience.Options` | Contextual metadata passed to `OnRetry` callbacks (`AttemptNumber`, `Delay`, `Exception`, `Result`). | `ResilienceContext` | Structured logging and telemetry on each retry attempt. | Intermediate | Yes (`Level01RetryAndCircuitBreakers`, `Cookbook Recipe 1`) |
| `RetryStrategyOptions` | `EricksonLopez.Resilience.Options` | Retry configuration options (`MaxRetryAttempts`, `Delay`, `BackoffType`, `MaxDelay`, `ShouldHandle`). | `ResilienceStrategyOptions` | Recovering from transient network, socket, or database errors. | Basic | Yes (`Level01RetryAndCircuitBreakers` through `Level10EnterpriseArchitecture`) |
| `TimeoutContext` | `EricksonLopez.Resilience.Options` | Contextual metadata delivered when an operation exceeds its configured deadline. | `ResilienceContext` | Diagnostic callbacks and alerts on SLA deadline expiration. | Basic | Yes (`Level01RetryAndCircuitBreakers`, `Level09HttpAspnetcoreOpentelemetry`) |
| `TimeoutStrategyOptions` | `EricksonLopez.Resilience.Options` | Timeout configuration options (`Timeout`, `OnTimeout`). | `ResilienceStrategyOptions` | Enforcing strict execution limits on blocking operations. | Basic | Yes (`Level01RetryAndCircuitBreakers` through `Level10EnterpriseArchitecture`) |
| `IResiliencePolicy` | `EricksonLopez.Resilience.Policies` | Contract for modular, reusable declarative domain policy definitions. | `IResiliencePipelineBuilder` | Encapsulating reusable typed resilience policies across microservices. | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`, `Level11ComprehensiveCoverage`) |
| `ResiliencePolicy` | `EricksonLopez.Resilience.Policies` | Abstract base class implementing `IResiliencePolicy`. | `IResiliencePolicy` | Facilitates authoring enterprise resilience policy classes. | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 6`) |

---

## 2. EricksonLopez.Resilience (Core — L2)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `ResiliencePipelineBuilder` | `EricksonLopez.Resilience.Builder` | Concrete builder for untyped pipelines and compilation factory management. | `Abstractions` | Imperative pipeline composition and runtime compilation. | Intermediate | Yes (`Level01RetryAndCircuitBreakers`, `Level11ComprehensiveCoverage`) |
| `ResiliencePipelineBuilder<TResult>` | `EricksonLopez.Resilience.Builder` | Concrete builder for strongly-typed pipelines with Fallback and Hedging support. | `Abstractions` | Composing pipelines with a specific return type. | Advanced | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 8, 10, 19`) |
| `ResiliencePipelineRegistry` | `EricksonLopez.Resilience.Registry` | Thread-safe, lock-free in-memory implementation of `IResiliencePipelineRegistry`. | `Abstractions` | High-throughput concurrent storage and retrieval of compiled pipelines. | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`, `Level11ComprehensiveCoverage`) |
| `ResiliencePolicyRegistry` | `EricksonLopez.Resilience.Registry` | Central registry storing declarative `IResiliencePolicy` definitions prior to compilation. | `Abstractions` | Policy registration and pre-compilation inspection. | Intermediate | Yes (`Level08CustomPoliciesTypedPipelines`) |
| `PassthroughResiliencePipeline` | `EricksonLopez.Resilience.Pipelines` | Zero-overhead untyped pipeline executing delegates directly without strategies. | `Abstractions` | Unit testing dependent application services without simulating Polly. | Basic | Yes (`Level08CustomPoliciesTypedPipelines`, `docs/testing.md`) |
| `PassthroughResiliencePipeline<TResult>` | `EricksonLopez.Resilience.Pipelines` | Zero-overhead typed pipeline for deterministic unit testing. | `Abstractions` | Unit testing application services consuming typed pipelines. | Basic | Yes (`Level08CustomPoliciesTypedPipelines`, `docs/testing.md`) |
| `ResultRetryClassifier` | `EricksonLopez.Resilience.Classification` | Singleton deterministic classifier evaluating `Result<T>`, `Error`, and transient exceptions. | `EricksonLopez.Result` | Automatic evaluation of retry eligibility without coupling to exceptions. | Intermediate | Yes (`Level06ErrorClassification`, `Cookbook Recipe 3`) |
| `TransientExceptionClassifier` | `EricksonLopez.Resilience.Classification` | Static utility detecting transient network, socket, and HTTP exceptions in .NET BCL. | `ResultRetryClassifier` | Rapid helpers in `ShouldHandle` predicates. | Basic | Yes (`Level06ErrorClassification`) |
| `ResiliencePipelineBuilderExtensions` | `EricksonLopez.Resilience.Extensions` | Builder extension methods (`AddResultRetry`, `AddStandardResilience`, `AddDatabaseResilience`, `AddTimeout`). | `Abstractions`, `Builder` | Turn-key, production-ready architectural presets. | Basic | Yes (`Level01RetryAndCircuitBreakers`, `Level04MediatorTransactions`) |

---

## 3. EricksonLopez.Resilience.Polly (Infrastructure Adapter — L4)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `PollyResilienceRegistration` | `EricksonLopez.Resilience.Polly.Registration` | Registers the Polly v8 engine as the active compilation factory for pipeline builders. | `Abstractions`, `Core`, `Polly` | Infrastructure initialization during application startup. | Intermediate | Yes (`Level00Introduction`, `Level08CustomPoliciesTypedPipelines`) |
| `PollyContextAdapter` | `EricksonLopez.Resilience.Polly.Adapters` | Bidirectional adapter bridging `ResilienceContext` and `Polly.ResilienceContext` with pooling. | `Abstractions`, `Polly` | Low-level interoperability with the Polly execution engine. | Advanced | Yes (`Level11ComprehensiveCoverage`) |
| `PollyPipelineBuilderTranslator` | `EricksonLopez.Resilience.Polly.Builders` | Translates declarative ecosystem options into compiled Polly v8 `ResiliencePipeline` instances. | `Abstractions`, `Core`, `Polly` | High-efficiency compilation without reflection or boxing. | Advanced | Yes (`Level11ComprehensiveCoverage`) |
| `PollyResiliencePipeline` | `EricksonLopez.Resilience.Polly.Adapters` | Adapter implementing `IResiliencePipeline` by executing an underlying Polly v8 pipeline. | `Abstractions`, `Polly` | Operation execution through the Polly engine. | Advanced | Yes (`Level00Introduction` through `Level10EnterpriseArchitecture`) |
| `PollyResiliencePipeline<TResult>` | `EricksonLopez.Resilience.Polly.Adapters` | Adapter implementing `IResiliencePipeline<TResult>` by executing a typed Polly v8 pipeline. | `Abstractions`, `Polly` | Strongly-typed operation execution through Polly. | Advanced | Yes (`Level08CustomPoliciesTypedPipelines`, `Cookbook Recipe 8, 10, 19`) |
| `PollyResilienceExecutor` | `EricksonLopez.Resilience.Polly.Adapters` | Implementation of `IResilienceExecutor` with structured logging and OpenTelemetry tracing. | `Abstractions`, `OpenTelemetry`, `Microsoft.Extensions.Logging` | Primary execution facade injected into DI containers. | Advanced | Yes (`Level00Introduction` through `Level10EnterpriseArchitecture`) |

---

## 4. EricksonLopez.Resilience.DependencyInjection (Host / Integration — L3)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `ResilienceOptions` | `EricksonLopez.Resilience.DependencyInjection` | Options model for configuring policies during `AddEricksonLopezResilience`. | `Abstractions` | Startup policy registration and configuration binding. | Basic | Yes (`Level11ComprehensiveCoverage`) |
| `ResilienceServiceCollectionExtensions` | `EricksonLopez.Resilience.DependencyInjection` | `IServiceCollection` extensions (`AddEricksonLopezResilience`, `AddResiliencePolicy`). | `Microsoft.Extensions.DependencyInjection` | Registering resilience infrastructure, policies, and executors into IoC containers. | Basic | Yes (`Level00Introduction`, `Cookbook Recipe 17`) |
| `ResilienceConfigurationExtensions` | `EricksonLopez.Resilience.DependencyInjection` | Reflection-free, Native AOT-safe options binding from `IConfigurationSection`. | `Microsoft.Extensions.Configuration` | Dynamic policy configuration loaded from `appsettings.json`. | Intermediate | Yes (`Level02RateLimitingAndHedging`, `Cookbook Recipe 9`) |

---

## 5. EricksonLopez.Resilience.Mediator (CQRS Integration — L3)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `IResilientRequest` | `EricksonLopez.Resilience.Mediator.Contracts` | Marker contract for Mediator requests declaring an associated resilience policy name. | BCL | Declarative association of commands and queries with policies without reflection. | Basic | Yes (`Level04MediatorTransactions`, `Level10EnterpriseArchitecture`, `Cookbook Recipe 4`) |
| `ResiliencePipelineBehavior<TRequest, TResponse>` | `EricksonLopez.Resilience.Mediator.Behaviors` | Open pipeline behavior intercepting and executing requests implementing `IResilientRequest`. | `EricksonLopez.Mediator`, `Abstractions` | Automatic resilience application across CQRS handlers via zero-allocation struct continuations. | Intermediate | Yes (`Level04MediatorTransactions`, `Level10EnterpriseArchitecture`) |
| `MediatorResilienceExtensions` | `EricksonLopez.Resilience.Mediator.Extensions` | Extension method `AddResiliencePipelineBehavior` for `IServiceCollection`. | `Microsoft.Extensions.DependencyInjection` | Registering Mediator resilience behaviors in DI. | Basic | Yes (`Level04MediatorTransactions`, `Cookbook Recipe 4`) |

---

## 6. EricksonLopez.Resilience.OpenTelemetry (Observability — L3)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `ResilienceActivitySource` | `EricksonLopez.Resilience.OpenTelemetry` | Distributed tracing instrumentation (`ActivitySource`) with `ExceptionSanitizer` support. | `System.Diagnostics` | Emitting distributed spans and semantic tags for protected operations. | Intermediate | Yes (`Level09HttpAspnetcoreOpentelemetry`, `Cookbook Recipe 18`) |
| `ResilienceMeter` | `EricksonLopez.Resilience.OpenTelemetry` | OpenTelemetry metrics instrumentation emitting latency histograms and event counters. | `System.Diagnostics.Metrics` | Prometheus/Grafana dashboards, SLA tracking, and degradation alerts. | Intermediate | Yes (`Level09HttpAspnetcoreOpentelemetry`, `Level10EnterpriseArchitecture`) |
| `OpenTelemetryResilienceExtensions` | `EricksonLopez.Resilience.OpenTelemetry.Extensions` | Fluent `WithTelemetry()` extension methods for Retry, CircuitBreaker, and Timeout options. | `Abstractions` | Attaching metrics and tracing hooks to individual strategy options. | Basic | Yes (`Level09HttpAspnetcoreOpentelemetry`) |

---

## 7. EricksonLopez.Resilience.AspNetCore (Presentation & Web — L3)

| Name | Namespace | Responsibility | Dependencies | Use Cases | Complexity | Existing Example |
|---|---|---|---|---|---|---|
| `AspNetCoreResilienceExtensions` | `EricksonLopez.Resilience.AspNetCore.Extensions` | `RequireResilience` extension assigning policy metadata to ASP.NET Core endpoints. | `Microsoft.AspNetCore.Builder` | Declarative routing and documentation of resilience in Minimal APIs. | Basic | Yes (`Level09HttpAspnetcoreOpentelemetry`, `Level11ComprehensiveCoverage`) |
| `CircuitBreakerHealthCheckExtensions` | `EricksonLopez.Resilience.AspNetCore.Extensions` | `AddCircuitBreakerCheck` extension registering circuit breaker state health checks. | `Microsoft.Extensions.Diagnostics.HealthChecks` | Liveness and readiness endpoints for Kubernetes and cloud load balancers. | Intermediate | Yes (`Level11ComprehensiveCoverage`) |
| `HttpClientResilienceExtensions` | `EricksonLopez.Resilience.AspNetCore.Extensions` | `AddResiliencePolicy` and `AddStandardResilienceHandler` extensions for `IHttpClientBuilder`. | `Microsoft.Extensions.DependencyInjection` | Comprehensive resilience protection for outbound `HttpClient` requests. | Basic | Yes (`Level09HttpAspnetcoreOpentelemetry`, `Cookbook Recipe 5`) |
| `ResilienceCircuitBreakerHealthCheck` | `EricksonLopez.Resilience.AspNetCore.HealthChecks` | `IHealthCheck` implementation reporting Circuit Breaker state (`Healthy`, `Degraded`, `Unhealthy`). | `Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks` | Real-time circuit state health monitoring. | Intermediate | Yes (`Level11ComprehensiveCoverage`) |
| `ResilienceDelegatingHandler` | `EricksonLopez.Resilience.AspNetCore.Http` | HTTP `DelegatingHandler` executing outbound requests through an `IResilienceExecutor`. | `Abstractions`, `System.Net.Http` | Transparent resilience wrapping for HTTP clients. | Intermediate | Yes (`Level09HttpAspnetcoreOpentelemetry`, `Cookbook Recipe 5`) |
| `ResilienceEndpointMetadata` | `EricksonLopez.Resilience.AspNetCore.Metadata` | Endpoint metadata storing the resilience policy name associated with an HTTP endpoint. | BCL | Endpoint inspection and middleware-level resilience telemetry. | Basic | Yes (`Level09HttpAspnetcoreOpentelemetry`) |
