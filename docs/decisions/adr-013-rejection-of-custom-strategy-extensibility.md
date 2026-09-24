# ADR-013: Rejection of Custom Strategy Extensibility (ResilienceStrategy<T>)

## Status
Rejected

## Date
2026-09-04

## Context
Polly v8 exposes `ResilienceStrategy<T>` as an extension point that allows consumers to implement entirely custom resilience strategies and add them to a pipeline builder. This capability was evaluated for inclusion in `EricksonLopez.Resilience` to allow developers to author proprietary strategies using the ecosystem's builder API.

## Decision
**`EricksonLopez.Resilience` will not expose `ResilienceStrategy<T>` or any equivalent custom strategy extension point in its public API.**

Rationale:
1. **API Surface Explosion**: Exposing a custom strategy interface multiplies the public API surface by the number of possible strategy implementations. Each custom strategy becomes a maintenance burden and a potential source of AOT trimming warnings.
2. **AOT Incompatibility Risk**: Custom strategies using generic type parameters require `[DynamicallyAccessedMembers]` annotations throughout. Third-party implementations cannot be validated for Native AOT compliance, creating invisible trimming hazards.
3. **Polly Coupling Exposure**: Exposing `ResilienceStrategy<T>` would inevitably require surfacing Polly's internal execution delegate types (`ExecutionPredicateArguments<T>`, `OutcomeArguments<T, TArgs>`) in the public abstraction layer, directly violating ADR-001's L0 decoupling invariant.
4. **Escape Hatch Available**: Any developer requiring a custom strategy can implement it directly against `Polly.Core` in a `EricksonLopez.Resilience.Polly`-layer extension. This preserves the capability without contaminating the abstraction layer.
5. **Composition via Fallback/Hedging**: The combination of `FallbackStrategyOptions<TResult>`, `HedgingStrategyOptions<TResult>`, and `ShouldHandle` predicates covers the vast majority of custom strategy use cases without requiring a raw strategy interface.

## Consequences
### Positive
- Public API surface remains bounded and auditable.
- Native AOT compliance is guaranteed for all strategies.
- L0 decoupling from Polly is preserved without leakage.

### Negative / Tradeoffs
- Developers with highly specific strategy requirements (e.g., bulkhead isolation, deadline propagation) must implement these directly via Polly in a custom infrastructure layer.
