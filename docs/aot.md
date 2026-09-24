# Native AOT & Trimming Compatibility

## 1. Native AOT & Trimming Mandate

`EricksonLopez.Resilience` is designed from the ground up to be **100% Native AOT & Trimming Compatible** across .NET 8, .NET 9, and .NET 10.

In `Directory.Build.props`:

```xml
<PropertyGroup>
  <IsAotCompatible Condition="'$(IsAotCompatible)' == ''">true</IsAotCompatible>
  <EnableTrimAnalyzer Condition="'$(EnableTrimAnalyzer)' == ''">true</EnableTrimAnalyzer>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

---

## 2. Zero Reflection Design & Polly Engine Translation

Dynamic reflection and runtime code generation break Native AOT compilation. To ensure complete trimming safety:

1. **Polly Pipeline Translation**: Strategy configurations (`RetryStrategyOptions`, `CircuitBreakerStrategyOptions`, `TimeoutStrategyOptions`, `RateLimiterStrategyOptions`, `FallbackStrategyOptions<TResult>`, `HedgingStrategyOptions`, `HedgingStrategyOptions<TResult>`) are mapped directly using strongly-typed factory delegates and strongly-typed predicates.
2. **Context Passing**: `ResilienceContext` stores and retrieves state without reflection using type checks and direct property access.
3. **OpenTelemetry Semantics**: `ResilienceMeter` and `ResilienceActivitySource` use static .NET BCL `Meter` and `ActivitySource` APIs with static tag structures.
4. **Configuration Binding**: `ResilienceConfigurationExtensions` uses explicit scalar parsers (`int.TryParse`, `double.TryParse`, `TimeSpan.TryParse`) for numeric and time-span options (ADR-009). Enum-typed options (`BackoffType`, `RateLimiterType`) use `Enum.TryParse<T>` with compile-time-known generic parameters — AOT-compatible in .NET 8+ via generic specialization, emitting zero IL2026/IL3050 trimming warnings.
5. **Database Exception Classification**: `ResultRetryClassifier.IsDatabaseDeadlockOrTransient` uses best-effort property-reflection (`GetProperty("Number")`, `GetProperty("SqlState")`) on optional third-party ORM exception types (`SqlException`, `NpgsqlException`, `MySqlException`). This is explicitly suppressed with `[UnconditionalSuppressMessage("Trimming", "IL2075")]`. The method is AOT-safe and gracefully returns `false` (non-retryable classification) if ORM types are trimmed.

---

## 3. Dedicated Native AOT Smoke Test Suite

To validate execution under CoreRT/ILC native compilation without test runner reflection interference, the dedicated project [`EricksonLopez.Resilience.AotSmokeTest`](../tests/EricksonLopez.Resilience.AotSmokeTest/EricksonLopez.Resilience.AotSmokeTest.csproj) compiles and executes as a standalone native binary:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PublishAot>true</PublishAot>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

### Verification Command

```bash
dotnet publish tests/EricksonLopez.Resilience.AotSmokeTest/EricksonLopez.Resilience.AotSmokeTest.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o ./aot-output

./aot-output/EricksonLopez.Resilience.AotSmokeTest
```

### Verified Assertions (22 / 22 Passing)
1. `ResilienceContext` invariants, property retrieval, and immutability.
2. `Result<T>` and `Error` transient retry classification.
3. Polly pipeline retry execution with decorrelated jitter.
4. Circuit breaker trip (`CircuitBrokenException`) and timeout rejections (`ResilienceTimeoutException`).
5. Dependency Injection registry compilation and `IResilienceExecutor` execution.
6. Mediator `ResiliencePipelineBehavior` execution.
7. OpenTelemetry `Meter` and `ActivitySource` diagnostics.
8. Typed pipeline execution with graceful `FallbackStrategyOptions<TResult>`.

Expected result: **0 Trimming Warnings (IL2026/IL3050), 0 AOT Compatibility Warnings, native executable exits with code 0**.
