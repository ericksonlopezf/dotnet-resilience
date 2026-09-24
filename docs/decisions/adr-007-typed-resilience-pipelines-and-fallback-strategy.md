# ADR-007: Typed Resilience Pipelines and Fallback Strategy

## Status
Accepted

## Date
2026-09-04

## Context
Non-generic resilience pipelines (`IResiliencePipeline`) handle untyped operations and return arbitrary values via generic `ExecuteAsync<TResult>` methods. However, specific resilience patterns—most notably **Fallback** (returning a cached value or degraded response upon failure) and **Typed Hedging**—require compile-time knowledge of the returned result type `TResult` to:
- Evaluate result outcomes against typed predicates.
- Generate alternative typed fallback outcomes (`ValueTask<TResult>`).
- Decouple typed pipeline compilation from untyped runtime reflection.

## Decision
1. **Generic Pipeline Contract (`IResiliencePipeline<TResult>`)**:
   - Introduce `IResiliencePipeline<TResult>` in `EricksonLopez.Resilience.Abstractions`, providing typed execution methods (`ExecuteAsync(Func<ResilienceContext, ValueTask<TResult>>, ...)`).
2. **First-Class Fallback Strategy (`FallbackStrategyOptions<TResult>`)**:
   - Provide strongly-typed options specifying `FallbackAction: Func<FallbackContext, ValueTask<TResult>>`, `ShouldHandleException`, `ShouldHandleResult`, and `OnFallback` callback.
3. **Polly Adapter Translation**:
   - `PollyPipelineBuilderTranslator` maps `ResiliencePipelineBuilder<TResult>` directly to `global::Polly.ResiliencePipeline<TResult>` using `Polly.AddFallback(...)`.
4. **Test Double Parity**:
   - Provide `PassthroughResiliencePipeline<TResult>` in `EricksonLopez.Resilience` for zero-dependency unit testing.

## Consequences
### Positive
- Enables first-class graceful degradation patterns across services (cache-aside fallbacks, default degraded DTOs).
- Preserves full Native AOT compatibility without dynamic reflection over generic return types.
- Completely decouples application code from `Polly.Fallback` types.

### Negative / Tradeoffs
- Pipeline registry and builders must support both non-generic (`IResiliencePipeline`) and typed (`IResiliencePipeline<TResult>`) variations.
