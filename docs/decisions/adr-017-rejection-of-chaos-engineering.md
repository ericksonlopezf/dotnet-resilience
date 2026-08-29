# ADR-017: Rejection of Chaos Engineering Support

## Status
**Rejected** — Out-of-scope for core library; delegate to dedicated tooling

## Context
Chaos engineering (fault injection) libraries such as Polly's `Simmy` extension allow developers to intentionally inject failures, latency, and faults into resilience pipelines for testing purposes. This was evaluated as a potential feature to aid in validating resilience configurations during development and CI.

## Decision
**Chaos engineering and fault injection will not be included in `EricksonLopez.Resilience`.**

Rationale:
1. **Testing Tool, Not Production Feature**: Chaos engineering is a development and testing discipline. Bundling fault injection into a production resilience library creates risk of accidental activation in non-test environments and confuses the library's primary purpose.
2. **Polly Simmy Already Exists**: `Polly.Simmy` (available via `Polly.Extensions.Simmy`) provides full chaos strategy support compatible with Polly v8, the underlying engine of this library. Users who need chaos testing can add Simmy to their test projects without any changes to `EricksonLopez.Resilience`.
3. **Separated Test Context**: Fault injection belongs in integration and chaos test projects, not in the production dependency graph. Maintaining it as a separate concern (e.g., a test-only `EricksonLopez.Resilience.Testing.Chaos` package) is the architecturally correct approach if future demand exists.
4. **Passthrough Pipeline for Tests**: `PassthroughResiliencePipeline` (part of the existing test utilities in `EricksonLopez.Resilience`) already satisfies the primary unit testing use case of disabling resilience in tests.

## Consequences
### Positive
- No chaos-specific code in the production dependency path.
- `PassthroughResiliencePipeline` remains the recommended test double.

### Negative / Tradeoffs
- Teams wanting chaos testing against ecosystem pipelines must compose `Polly.Simmy` directly or wait for a potential future `EricksonLopez.Resilience.Testing.Chaos` package.
