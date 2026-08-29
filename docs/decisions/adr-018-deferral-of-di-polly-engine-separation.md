# ADR-018: Deferral of DI/Polly Engine Separation (L1)

## Status
**Deferred** — Planned for v2.0.0 as a potentially breaking change

## Context
Currently, `EricksonLopez.Resilience.DependencyInjection` registers both the ecosystem abstractions **and** the Polly engine adapter via `PollyResilienceRegistration`. This means that calling `AddEricksonLopezResilience()` implicitly wires Polly as the execution engine. While ADR-001 guarantees that Application Layer code never references Polly directly, the DI registration package itself transitively depends on `EricksonLopez.Resilience.Polly`.

The ideal architecture would have two separate registrations:
- `AddEricksonLopezResilience()` → registers abstractions and pipeline registry only
- `AddPollyResilienceEngine()` (in `.Polly` package) → registers Polly as the concrete engine

## Decision
**This separation is deferred to v2.0.0.** The current coupling between the DI package and the Polly engine adapter is retained for v1.x.

Rationale:
1. **Breaking Change Risk**: Splitting the registration into two calls would require all existing consumers to update their `Program.cs` / `Startup.cs` registration code. This is a breaking change that must be batched with other v2.0.0 changes.
2. **Current Pragmatism**: In practice, 100% of deployments use Polly as the engine. The theoretical benefit of engine swappability (replace Polly with a hypothetical engine X) has zero documented consumer demand.
3. **ADR-001 Invariant Is Already Met**: Application Layer code has zero Polly references — verified by 6 architecture tests in CI. The L0 decoupling goal is achieved without requiring the DI split.
4. **Complexity vs. Value**: The internal refactoring to split registration while maintaining backward compatibility via deprecation notices, migration guides, and version negotiation is non-trivial engineering work with no current user benefit.

## Revisit Criteria
- v2.0.0 major version release window (natural breaking change opportunity).
- Documented user request for engine-swappability (e.g., integration with a non-Polly engine).
- `ArchitectureRulesTests` updated to enforce that `DependencyInjection` package does not reference `Polly` directly.
