# ADR-014: Rejection of Circuit Breaker Pool per URL Authority

## Status
Rejected

## Date
2026-09-04

## Context
`Microsoft.Extensions.Resilience` (MER) includes a `SelectPipelineBy` / `SelectPipelineByAuthority()` mechanism that routes HTTP requests to per-URL-authority circuit breaker pools. This prevents a single slow domain from opening a circuit breaker that affects all requests. The feature was evaluated for inclusion in `EricksonLopez.Resilience`.

## Decision
**Per-URL-authority circuit breaker pooling will not be implemented in `EricksonLopez.Resilience`.**

Rationale:
1. **Responsibility Boundary**: URL authority-based routing is a concern of the HTTP client dispatch layer, not the resilience library. Implementing it in the core library mixes HTTP routing semantics into a domain-agnostic resilience abstraction.
2. **Implementation Complexity vs. Value**: The feature requires internal keying logic, DI-aware pipeline selection, and synchronized circuit breaker state per key — significant complexity that benefits only a narrow set of HTTP gateway scenarios.
3. **Composable Alternative**: Any consumer needing per-authority isolation can register multiple named pipelines (one per authority) and select the appropriate pipeline name before invoking `IResilienceExecutor.ExecuteAsync(policyName, ...)`. This achieves the same isolation with no additional library complexity:
   ```csharp
   var policyName = $"http-{uri.Authority}"; // e.g., "http-api.example.com"
   await executor.ExecuteAsync(policyName, operation, context, ct);
   ```
4. **Scope Creep**: Adding this would make `EricksonLopez.Resilience` partially responsible for HttpClient infrastructure, creating overlap with `EricksonLopez.Resilience.AspNetCore` and competing with `Microsoft.Extensions.Http.Resilience`.

## Consequences
### Positive
- Library scope remains resilience-focused, not HTTP-routing-focused.
- No hidden per-host state or dictionary lookups on hot execution paths.

### Negative / Tradeoffs
- Developers requiring per-authority CB isolation must implement the selection logic in their own HttpClient policy or use MER's `SelectPipelineByAuthority` extension.
