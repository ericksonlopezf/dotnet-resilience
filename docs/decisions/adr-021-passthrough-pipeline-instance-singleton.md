# ADR-021: PassthroughResiliencePipeline Static Instance Singleton

## Status
Accepted

## Date
2026-09-15

## Context
`PassthroughResiliencePipeline` and `PassthroughResiliencePipeline<TResult>` are testing helpers that execute delegates directly, bypassing all resilience strategy evaluation. They require a `string name` constructor parameter to satisfy the `IResiliencePipeline.Name` contract.

The public README and `docs/testing.md` documentation included examples using `PassthroughResiliencePipeline.Instance` as a zero-configuration static singleton for unit test suites. However, no such property existed in the code — the examples were non-compilable, producing `CS0117` errors.

## Decision
Add a static `Instance` singleton property to `PassthroughResiliencePipeline` (untyped) with the name `"passthrough"`:

```csharp
public static PassthroughResiliencePipeline Instance { get; } = new("passthrough");
```

For `PassthroughResiliencePipeline<TResult>` (generic), a universal static `Instance` is not practical because each `TResult` would require a separate singleton. Consumers should construct named instances directly:

```csharp
// For typed pipelines in unit tests:
IResiliencePipeline<Result<T>> pipeline = new PassthroughResiliencePipeline<Result<T>>("test");
```

This decision is documented in the XML `<remarks>` of the typed constructor, referencing the untyped `Instance` for the common case.

## Rationale
1. **Consistency with documented API**: The README unit testing section is a primary developer onboarding path. A non-compilable example is a critical regression in developer experience.
2. **Low risk**: `PassthroughResiliencePipeline` is stateless and name-only; a shared singleton with name `"passthrough"` has no state sharing risk.
3. **Thread-safety**: The singleton initializer (`= new("passthrough")`) is guaranteed thread-safe by the .NET static field initialization model.
4. **API surface**: The property type is `PassthroughResiliencePipeline` (not `IResiliencePipeline`), preserving the concrete type for callers that need it while still being assignable to `IResiliencePipeline`.

## Consequences
### Positive
- Unit test examples in README and `docs/testing.md` now compile without modification.
- Eliminates the `CS0117` compilation error for users who copied the documented pattern.
- Provides a zero-argument zero-configuration test double: `IResiliencePipeline pipeline = PassthroughResiliencePipeline.Instance;`.

### Negative / Tradeoffs
- Minor API surface addition. The static property is on the concrete class, not the interface.
- Typed pipeline (`PassthroughResiliencePipeline<TResult>`) does not have an `Instance` property — this asymmetry is acceptable because generic static singletons would require per-TResult instances.
