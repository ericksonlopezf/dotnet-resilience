# ADR-009: AOT-Safe Configuration Binding for Resilience Options

## Status
Accepted

## Date
2026-09-04

## Context
Enterprise applications frequently configure resilience parameters (retry counts, backoff intervals, circuit breaker thresholds, and timeouts) via external configuration providers (`appsettings.json`, environment variables, Azure App Configuration). Traditional reflection-based configuration binders (`ConfigurationBinder.Bind`) are incompatible with Native AOT and IL trimming because property setters and types may be trimmed away at compile time.

## Decision
1. **Explicit Static Property Binding**:
   - `ResilienceConfigurationExtensions` provides strongly-typed, AOT-safe binding methods (`BindRetryOptions`, `BindCircuitBreakerOptions`, `BindTimeoutOptions`, `BindRateLimiterOptions`) that map directly from `IConfigurationSection` using non-reflective scalar parsers (`int.TryParse`, `double.TryParse`, `TimeSpan.TryParse`).
   - Enum-typed options (`BackoffType`, `RateLimiterType`) are resolved using `Enum.TryParse<T>` with compile-time-known generic type parameters and `ignoreCase: true`. This avoids `Enum.GetNames()` or `Enum.GetValues()` reflection and is AOT-compatible in .NET 8+ via generic specialization, generating no IL2026/IL3050 trimming warnings.
2. **Trim & AOT Compatibility**:
   - All configuration methods are verified for Native AOT compliance, generating zero IL trimming warnings and avoiding dynamic type discovery.
3. **Fail-Fast Validation**:
   - Bound options are validated against strategy bounds during pipeline registration, throwing explicit `ResilienceConfigurationException` on invalid values.

## Consequences
### Positive
- Enables configuration of resilience policies via `appsettings.json` and external configuration stores.
- 100% compliant with Native AOT and IL trimming in .NET 10.
- Clear, immediate validation errors when configuration values are misconfigured.

### Negative / Tradeoffs
- Requires maintaining explicit property binding helpers and switch expressions for each options class and each enum property.
