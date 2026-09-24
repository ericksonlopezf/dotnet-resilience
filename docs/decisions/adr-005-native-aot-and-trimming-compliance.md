# ADR-005: Native AOT and Trimming Compliance

## Status
Accepted

## Date
2026-09-04

## Context
Cloud-native deployments increasingly target Native AOT (Ahead-of-Time compilation) and aggressive assembly trimming for ultra-fast startup times (<20ms) and minimal container image footprints. Reflection-heavy architectures and unconstrained dynamic type dispatch cause runtime failures when compiled under Native AOT.

## Decision
1. **Centralized MSBuild Properties**:
   - `Directory.Build.props` enforces:
     ```xml
     <IsAotCompatible>true</IsAotCompatible>
     <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
     <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
     ```
2. **Reflection Strategy**:
   - Strategy builders, pipeline translators, and context adapters rely exclusively on strongly-typed delegates, generic constraints, and statically-bound factories — no dynamic dispatch.
   - Configuration binding (`ResilienceConfigurationExtensions`) uses reflection-free scalar parsers (`int.TryParse`, `double.TryParse`, `TimeSpan.TryParse`) for numeric and time-span options. Enum-typed options (`BackoffType`, `RateLimiterType`) use `Enum.TryParse<T>` with compile-time-known generic type parameters, which is AOT-compatible via .NET 8+ generic specialization and does not emit IL2026/IL3050 trim warnings.
   - Database deadlock classification (`ResultRetryClassifier.IsDatabaseDeadlockOrTransient`) uses best-effort property-reflection on `SqlException`, `NpgsqlException`, and `MySqlException` types, explicitly suppressed with `[UnconditionalSuppressMessage("Trimming", "IL2075")]`. This path is AOT-safe and degrades gracefully if third-party ORM types are trimmed — the classifier returns `Undetermined` and lets the strategy apply its default behavior.
3. **Automated AOT Verification**:
   - Dedicated smoke test project `EricksonLopez.Resilience.AotSmokeTest` verifies trim compatibility and Native AOT runtime correctness on every build.

## Consequences
### Positive
- 100% Native AOT compatible with zero trimming warnings across all packages (IL2026/IL3050 suppressed where justified with documented rationale).
- Minimal binary size and instant cold-start execution in serverless/container environments.
- High developer confidence enforced through compile-time static analysis.

### Negative / Tradeoffs
- Prohibits the use of arbitrary reflection-based dynamic policy discovery.
- Database deadlock classification for third-party ORM exceptions (`SqlException`, `NpgsqlException`, `MySqlException`) uses suppressed reflection and may not detect all transient codes if ORM types are trimmed; consumers should supplement with explicit `ShouldHandleException` predicates.
