# Changelog

All notable changes to the `EricksonLopez.Resilience` ecosystem will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-09-24

### Breaking Changes

- **BC-001 (Runtime/Behavioral): Removed fallback to `PassthroughResiliencePipeline` in `ResiliencePipelineBuilder.Build()`.**
  - **Previous State:** Calling `builder.Build()` without registering an execution engine adapter (Polly) silently returned a `PassthroughResiliencePipeline(Name)` fallback instance.
  - **Current State:** Calling `builder.Build()` without an initialized engine throws `InvalidOperationException` with explicit remediation instructions.
  - **Affected Consumers:** Applications or standalone tests calling `builder.Build()` without initializing `EricksonLopez.Resilience.Polly`.
  - **Migration:** Initialize Polly before building pipelines by calling `PollyResilienceRegistration.Initialize()` or `services.AddEricksonLopezResilience()`. For unit tests desiring passthrough execution without resilience strategies, use `PassthroughResiliencePipeline.Instance` directly.

- **BC-002 (Runtime/Behavioral): Removed fallback to `PassthroughResiliencePipeline<TResult>` in `ResiliencePipelineBuilder<TResult>.Build()`.**
  - **Previous State:** Calling `builder.Build()` on a typed builder without a registered typed compilation factory returned a `PassthroughResiliencePipeline<TResult>(builder.Name)` fallback instance.
  - **Current State:** Calling `builder.Build()` without an initialized typed factory throws `InvalidOperationException` with type details.
  - **Affected Consumers:** Applications or unit tests building typed pipelines without registering typed Polly adapters.
  - **Migration:** Register typed pipelines via `PollyResilienceRegistration.RegisterTypedPipeline<TResult>()` or DI `services.AddResiliencePipeline<TResult>()`. In unit test suites, instantiate `new PassthroughResiliencePipeline<TResult>(name)` directly.

- **BC-003 (Configuration/Behavioral): Enforced minimum `BreakDuration >= 500ms` and `NaN`/`Infinity` validation in `ResilienceStrategyValidator`.**
  - **Previous State:** Any `BreakDuration > TimeSpan.Zero` (including sub-500ms durations like 50ms or 100ms) was permitted, and floating-point `NaN`/`Infinity` in `FailureRatio` were not checked.
  - **Current State:** `ResilienceStrategyValidator.ValidateCircuitBreakerOptions` throws `ResilienceConfigurationException` if `BreakDuration < TimeSpan.FromMilliseconds(500)` or if `FailureRatio` is `NaN` or `Infinity`.
  - **Affected Consumers:** Consumers or automated test suites configuring fast-recovering circuit breakers with `BreakDuration` under 500ms.
  - **Migration:** Update `CircuitBreakerStrategyOptions.BreakDuration` to at least `TimeSpan.FromMilliseconds(500)`.

- **BC-004 (Observability/Behavioral): Metric tag `resilience.tenant_id` omitted by default to prevent cardinality explosion.**
  - **Previous State:** `ResilienceMeter` unconditionally attached the `resilience.tenant_id` tag to all emitted metrics whenever `tenantId` was not null or empty.
  - **Current State:** `resilience.tenant_id` is omitted by default and gated behind `ResilienceMeter.IncludeTenantIdTag` (defaults to `false`).
  - **Affected Consumers:** Multi-tenant monitoring dashboards, Prometheus/Grafana queries, and alerting rules grouping by `resilience.tenant_id`.
  - **Migration:** Set `ResilienceMeter.IncludeTenantIdTag = true;` during application bootstrap if tenant-level metric dimension tracking is required.

- **BC-005 (Runtime/Performance): Mandatory HTTP request body buffering in `ResilienceDelegatingHandler`.**
  - **Previous State:** `ResilienceDelegatingHandler.SendAsync` forwarded outbound `HttpRequestMessage` without buffering `request.Content`.
  - **Current State:** `SendAsync` invokes `await request.Content.LoadIntoBufferAsync(...)` before executing the resilient pipeline to enable safe payload replay on retries.
  - **Affected Consumers:** Outbound HTTP clients streaming large payloads (e.g. multi-gigabyte file uploads via `StreamContent`).
  - **Migration:** Ensure sufficient memory is available for buffered request content, or bypass `ResilienceDelegatingHandler` for streaming endpoints requiring non-buffered transfer.

- **BC-006 (Compile-time/Binary/Lifecycle): Generic constraint `new()` removed and deferred DI activation in `AddResiliencePolicy<TPolicy>()`.**
  - **Previous State:** Method was constrained to `where TPolicy : class, IResiliencePolicy, new()` and executed `new TPolicy()` immediately during `ConfigureServices`.
  - **Current State:** Generic constraint `new()` removed and replaced with `[DynamicallyAccessedMembers(PublicConstructors)] TPolicy`. Registered as `services.AddSingleton<IResiliencePolicy, TPolicy>()` for constructor-injected DI resolution.
  - **Affected Consumers:** Callers relying on IL binary method signature compatibility or expecting immediate synchronous policy instantiation during registration.
  - **Migration:** Recompile consuming assemblies. Ensure any constructor dependencies required by `TPolicy` are registered in the DI service collection.

- **BC-007 (Behavioral): `ResultRetryClassifier` classifies database deadlocks and Polly timeout rejections as retryable.**
  - **Previous State:** Database deadlocks (SQL Server 1205/3960, PostgreSQL 40P01/40001, MySQL 1213) and `Polly.Timeout.TimeoutRejectedException` returned `RetryabilityDecision.Undetermined`.
  - **Current State:** Evaluated as `RetryabilityDecision.Retry` by inspecting exception properties and type names.
  - **Affected Consumers:** Workloads with non-idempotent database operations or pipelines that did not expect automated retry upon database deadlock or Polly timeout.
  - **Migration:** For non-idempotent database operations, customize the retry condition using `RetryStrategyOptions.ShouldHandleException` to exclude deadlock error numbers if manual compensation is required.

- **BC-008 (Behavioral): Silent omission of retry strategy in `PollyPipelineBuilderTranslator` when `MaxRetryAttempts <= 0`.**
  - **Previous State:** Passing `MaxRetryAttempts <= 0` caused Polly to throw `ArgumentOutOfRangeException`.
  - **Current State:** `PollyPipelineBuilderTranslator` returns early without configuring a retry strategy if `MaxRetryAttempts <= 0`.
  - **Affected Consumers:** Consumers expecting an exception when configuring zero or negative retry attempts.
  - **Migration:** Explicitly specify `MaxRetryAttempts >= 1` when retry behavior is desired.

- **BC-009 (Behavioral): In-place mutation and bi-directional synchronization of `ResilienceContext` state across Polly pipeline execution.**
  - **Previous State:** `AttemptNumber` remained `1` inside executed operations, and custom properties modified inside execution delegates were not written back to the caller's context.
  - **Current State:** `AttemptNumber` is synchronized with the current retry attempt, and custom properties set during execution are copied back to the caller's `ResilienceContext`.
  - **Affected Consumers:** Consumers assuming `ResilienceContext` remains strictly immutable or unmutated after calling `pipeline.ExecuteAsync`.
  - **Migration:** Treat `AttemptNumber` and `Properties` as stateful across execution attempts. If complete context isolation is required, clone the context before execution.

### Added
- **EricksonLopez.Resilience.AspNetCore**:
  - `CircuitBreakerHealthCheckExtensions.AddCircuitBreakerCheck`: Extension method on `IHealthChecksBuilder` to register real-time circuit breaker health monitoring (`Healthy`, `Degraded`, `Unhealthy`).
  - `ResilienceCircuitBreakerHealthCheck`: `IHealthCheck` implementation monitoring circuit breaker state transitions.
- **EricksonLopez.Resilience**:
  - `PassthroughResiliencePipeline.Instance`: Static singleton property for unit testing without pipeline allocation.
  - `ResiliencePipelineBuilder<TResult>.AddDatabaseResilience`: Typed builder preset extension for database resilience.
- **EricksonLopez.Resilience.DependencyInjection**:
  - `ResilienceServiceCollectionExtensions.AddResiliencePolicy`: Overload accepting `Action<IResiliencePipelineBuilder, IServiceProvider>` providing dynamic access to `IServiceProvider`.
- **EricksonLopez.Resilience.OpenTelemetry**:
  - `ResilienceActivitySource.RecordException`: Helper to record structured exception telemetry on activities following OpenTelemetry semantic conventions.
  - `ResilienceActivitySource.ExceptionSanitizer`: Configurable delegate to redact sensitive information from recorded exception messages and stack traces.
  - `ResilienceMeter.RecordCircuitBreakerRejection`: New metric recording circuit breaker rejection count.
  - `ResilienceMeter.IncludeTenantIdTag`: Property controlling multi-tenant tag emission to avoid metric cardinality explosion.
- **EricksonLopez.Resilience.Abstractions**:
  - `ResilienceContext.WithCancellationToken`: Creates a context clone with an updated `CancellationToken` while preserving correlation metadata and custom properties.
  - `ResilienceContext.CopyPropertiesTo`: Public method for thread-safe property copying between contexts.
  - Explicit public constructors with XML documentation across all strategy options (`RetryStrategyOptions`, `CircuitBreakerStrategyOptions`, `TimeoutStrategyOptions`, `RateLimiterStrategyOptions`, `FallbackStrategyOptions<TResult>`, `HedgingStrategyOptions`, `HedgingStrategyOptions<TResult>`, `ResilienceOptions`, `ResiliencePipelineRegistry`, `ResiliencePolicyRegistry`, `ResultRetryClassifier`).
- **Documentation & Architecture**:
  - `ADR-020`: Memory Allocation Boundaries and Context Pooling Strategy — documents `ResilienceContext` per-execution instantiation design, Polly context pooling at the L4 layer, and allocation boundaries (~32–112 bytes per execution). Added on 2026-09-15 post-release as a retroactive architectural record.
  - `ADR-021`: PassthroughResiliencePipeline Static Instance Singleton — documents the decision to add the `Instance` static property to `PassthroughResiliencePipeline`, resolving the discrepancy between README unit testing examples and the actual API.

### Changed
- **NuGet & Package Discovery**:
  - Standardized and optimized `<PackageTags>` across all 7 NuGet packages and `Directory.Build.props`, establishing a clean hierarchical taxonomy (`dotnet;csharp;resilience;fault-tolerance;native-aot` as common ecosystem tags + dedicated package-specific tags).
  - Eliminated redundant synonyms (`.net`, `asp-net-core`, `di`), removed phantom feature tags (`middleware` in `AspNetCore` and `Mediator`), added missing implemented strategy and feature tags (`timeout` and `transient-fault-handling` in `Resilience`, `health-checks` and `minimal-apis` in `AspNetCore`, `clean-architecture` in `Abstractions` and `Mediator`), and replaced generic `pipeline` / `execution-engine` with canonical `resilience-pipeline`.
- **Documentation & Accuracy**:
  - `docs/aot.md`, `docs/decisions/adr-005-native-aot-and-trimming-compliance.md`, `docs/decisions/adr-009-aot-safe-configuration-binding.md`: Corrected description of enum configuration binding — `Enum.TryParse<T>` with compile-time-known types is used (AOT-compatible), not `switch` expressions.
  - `docs/aot.md`: Documents suppressed best-effort reflection in `ResultRetryClassifier.IsDatabaseDeadlockOrTransient` for ORM exception detection.

## [1.0.0] - 2026-08-29

### Added
- **EricksonLopez.Resilience.Abstractions**:
  - Core execution and pipeline contracts: `IResilienceExecutor`, `IResiliencePipeline`, `IResiliencePipeline<TResult>`, `IResiliencePipelineRegistry`, `IResiliencePolicy`, `IResiliencePipelineBuilder`.
  - Immutable `ResilienceContext` carrying operation names, correlation IDs, tenant IDs, attempt indices, cancellation tokens, and typed property bag.
  - Strongly-typed strategy options: `RetryStrategyOptions`, `CircuitBreakerStrategyOptions`, `TimeoutStrategyOptions`, `RateLimiterStrategyOptions`, `RateLimiterType` (`SlidingWindow`, `FixedWindow`, `TokenBucket`, `Concurrency`), `FallbackStrategyOptions<TResult>`, `FallbackContext`, `HedgingStrategyOptions`, `HedgingStrategyOptions<TResult>`.
  - Ecosystem exception hierarchy: `ResilienceException`, `ResiliencePolicyNotFoundException`, `ResilienceConfigurationException`, `ResilienceTimeoutException`, `CircuitBrokenException`, `RateLimitRejectedException`.
  - Error classification contracts: `IResultRetryClassifier`, `IErrorClassifier`, `RetryabilityDecision`.
- **EricksonLopez.Resilience**:
  - `ResultRetryClassifier` with rich evaluation of `Result<T>`, `ErrorRetryability` (`Transient` vs `Permanent`), `ErrorType` categories, and transient exception hierarchies.
  - `TransientExceptionClassifier` evaluating socket, HTTP, and timeout exceptions.
  - Fluent pipeline builders `ResiliencePipelineBuilder` and generic `ResiliencePipelineBuilder<TResult>` with validation constraints.
  - Pre-configured profiles: `AddStandardResilience()`, `AddDatabaseResilience()`, `AddResultRetry()`, `AddTimeout()`.
  - Testing helpers: `PassthroughResiliencePipeline` and `PassthroughResiliencePipeline<TResult>` for deterministic unit testing.
  - Registry abstractions: `ResiliencePipelineRegistry` and `ResiliencePolicyRegistry`.
- **EricksonLopez.Resilience.Polly**:
  - `PollyResiliencePipeline`, `PollyResiliencePipeline<TResult>`, and `PollyResilienceExecutor` translating ecosystem options into Polly v8+ execution engines.
  - `PollyPipelineBuilderTranslator` bridging fluent options to `Polly.ResiliencePipelineBuilder`.
  - `PollyContextAdapter` managing zero-allocation `ResilienceContext` mapping.
  - Exception translation mapping Polly exceptions (`TimeoutRejectedException`, `BrokenCircuitException`, `IsolatedCircuitException`, `RateLimiterRejectedException`) to first-party exceptions.
  - `PollyResilienceRegistration` enabling trim-safe registration for both untyped and typed pipelines.
- **EricksonLopez.Resilience.DependencyInjection**:
  - `AddEricksonLopezResilience()` and `AddResiliencePolicy()` extension methods with trim-safe automatic pipeline compilation.
  - AOT-safe configuration binders for `IConfigurationSection` (`AddResiliencePolicyFromConfiguration`, `BindRetryOptions`, `BindCircuitBreakerOptions`, `BindTimeoutOptions`, `BindRateLimiterOptions`). Scalar fields use reflection-free parsers; enum fields use `Enum.TryParse<T>` with compile-time known types (AOT-compatible in .NET 8+).
- **EricksonLopez.Resilience.Mediator**:
  - `IResilientRequest` marker contract.
  - Minimized-allocation struct-continuation `ResiliencePipelineBehavior<TRequest, TResponse>` (struct-constraint eliminates Boxing on continuation; resilient-path async wrapping incurs bounded async state machine allocation).
  - `AddResiliencePipelineBehavior()` DI extension.
- **EricksonLopez.Resilience.OpenTelemetry**:
  - Semantic metrics (`resilience.execution.duration`, `resilience.retry.attempts`, `resilience.circuit_breaker.state_changes`, `resilience.timeout.rejections`, `resilience.rate_limiter.rejections`).
  - Distributed tracing `ActivitySource` instrumentation with semantic span tags (`resilience.policy`, `resilience.operation`, `resilience.attempt`, `resilience.correlation_id`, `resilience.tenant_id`).
- **EricksonLopez.Resilience.AspNetCore**:
  - `ResilienceDelegatingHandler` and `AddResiliencePolicy()` for resilient HTTP clients.
  - `AddStandardResilienceHandler()` for turn-key HTTP client resilience configuration.
  - `RequireResilience()` endpoint metadata extension for ASP.NET Core Minimal APIs.
- **Testing, Verification & Governance**:
  - Multi-targeting .NET 8.0, .NET 9.0, and .NET 10.0 (`net8.0;net9.0;net10.0`).
  - Strong-named assemblies signed with official key (`EricksonLopez.snk`).
  - 10 test projects covering unit, integration, architecture, and Native AOT smoke testing (99 automated test executions per TFM).
  - 22 Native AOT assertions in standalone smoke test suite.
  - 95%+ Mutation score baseline across all 7 packages.
  - Architecture governance script (`scripts/verify-compliance.ps1`).
- **Documentation**:
  - 26 comprehensive kebab-case architectural reference guides in `docs/`.
  - 19 Architectural Decision Records (ADRs) in `docs/decisions/` (`ADR-001` through `ADR-019`).
  - 11 progressive showcase tutorials in `docs/showcase/` (`level-00` through `level-10`).

[Unreleased]: https://github.com/ericksonlopezf/dotnet-resilience/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/ericksonlopezf/dotnet-resilience/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/ericksonlopezf/dotnet-resilience/releases/tag/v1.0.0
