# ADR-001: Polly v8 Decoupling as L4 Infrastructure Detail

## Status
**Accepted**

## Context
In modern distributed systems, resilience is essential. However, directly referencing third-party libraries such as Polly across application handlers, domain models, and presentation controllers introduces tight coupling:
- Application handlers become dependent on external execution types (`Polly.ResiliencePipeline`, `Polly.ResilienceContext`).
- Testing requires mocking complex third-party execution structures.
- Upgrading resilience libraries across large microservices ecosystems becomes high-risk and costly.

## Decision
We decouple application and domain code completely from Polly:
1. **L0 Foundation (`EricksonLopez.Resilience.Abstractions`)**: Defines pure first-party contracts (`IResilienceExecutor`, `IResiliencePipeline`, `IResiliencePipelineBuilder`, `ResilienceContext`).
2. **L2 Core (`EricksonLopez.Resilience`)**: Implements policy configuration builders, thread-safe registries, and deterministic error classifiers.
3. **L4 Infrastructure (`EricksonLopez.Resilience.Polly`)**: Serves strictly as the execution engine adapter translating ecosystem policies into compiled Polly v8+ pipelines.
4. **Exception Translation**: All Polly exceptions (`TimeoutRejectedException`, `BrokenCircuitException`, `RateLimiterRejectedException`) are translated into first-party typed exceptions (`ResilienceTimeoutException`, `CircuitBrokenException`, `RateLimitRejectedException`).

## Consequences
### Positive
- Domain and Application layers have 0 dependencies on Polly.
- Unit testing is trivial using first-party test doubles like `PassthroughResiliencePipeline`.
- Upgrading or swapping execution engines requires changes only in the L4 adapter package.

### Negative / Tradeoffs
- Requires maintaining an adapter layer (`PollyPipelineBuilderTranslator`, `PollyResiliencePipeline`).
- Minor overhead in translating options and exceptions (sub-microsecond, optimized for Native AOT).
