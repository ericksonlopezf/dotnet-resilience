# Changelog

All notable changes to the `EricksonLopez.Resilience` ecosystem will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
  - Static configuration binders for `IConfigurationSection` (`ResilienceOptions`, `NamedPolicyRegistration`, `ConfigureResilience()`, `BindPolicy()`).
- **EricksonLopez.Resilience.Mediator**:
  - `IResilientRequest` marker contract.
  - Zero-allocation struct-continuation `ResiliencePipelineBehavior<TRequest, TResponse>`.
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
  - 100% Mutation score baseline across all 7 packages.
  - Architecture governance script (`scripts/verify-compliance.ps1`).
- **Documentation**:
  - 26 comprehensive kebab-case architectural reference guides in `docs/`.
  - 19 Architectural Decision Records (ADRs) in `docs/decisions/` (`ADR-001` through `ADR-019`).
  - 11 progressive showcase tutorials in `docs/showcase/` (`level-00` through `level-10`).

[Unreleased]: https://github.com/ericksonlopezf/dotnet-resilience/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/ericksonlopezf/dotnet-resilience/releases/tag/v1.0.0
