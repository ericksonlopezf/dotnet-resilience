# ADR-005: Native AOT and Trimming Compliance

## Status
**Accepted**

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
2. **Zero Runtime Reflection**:
   - Strategy builders, pipeline translators, and context adapters rely exclusively on strongly-typed delegates, generic constraints, and statically-bound factories.
   - Configuration binding (`ResilienceConfigurationExtensions`) uses explicit scalar parsers and switch expressions for enum resolution — no `Enum.TryParse<T>` reflection.
3. **Automated AOT Verification**:
   - Dedicated smoke test project `EricksonLopez.Resilience.AotSmokeTest` verifies trim compatibility and Native AOT runtime correctness on every build.

## Consequences
### Positive
- 100% Native AOT compatible with zero warnings across all packages.
- Minimal binary size and instant cold-start execution in serverless/container environments.
- High developer confidence enforced through compile-time static analysis.

### Negative / Tradeoffs
- Prohibits the use of arbitrary reflection-based dynamic policy discovery.
