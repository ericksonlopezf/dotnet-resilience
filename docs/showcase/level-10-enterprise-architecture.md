# Level 10: Enterprise Architecture — Mission-Critical Reference Solution

## 1. End-to-End Clean Architecture Overview
In an enterprise cloud microservice, resilience connects across all architectural boundaries:

```mermaid
graph TD
    Client["Client / API Gateway"] -->|HTTP POST| Ingress["ASP.NET Core Endpoint (.RequireResilience)"]
    Ingress -->|SendAsync| Mediator["Mediator Pipeline Behavior"]
    Mediator -->|Intercept| Executor["IResilienceExecutor (Policy: enterprise-checkout)"]
    Executor --> RateLimiter["1. Ingress Rate Limiter"]
    RateLimiter --> Timeout["2. Operation Timeout"]
    Timeout --> ResultRetry["3. ResultRetry (Exponential Jitter)"]
    ResultRetry --> CircuitBreaker["4. Circuit Breaker"]
    CircuitBreaker --> Handler["5. Command Handler (Unit of Work & Outbox)"]
    Handler --> Database[("Transactional SQL DB")]
```

---

## 2. Ingress to Persistence Flow
1. **Presentation Layer**: The HTTP request arrives with an `Idempotency-Key` and `Tenant-Id` header.
2. **Mediator Pipeline**: `ResiliencePipelineBehavior` wraps command execution with `IResilienceExecutor`.
3. **Resilience Strategies**:
   - **Rate Limiting**: Throttles quota per tenant.
   - **Timeout**: Enforces an SLA deadline.
   - **Retry & Circuit Breaker**: Evaluates returned `Result<T>` and handles transient database lock timeouts.
4. **Application Handler**:
   - Initializes a fresh database transaction for each retry attempt.
   - Preserves and checks the idempotency key.
   - Commits aggregate changes and writes event to Transactional Outbox atomically.
5. **Observability**: Metrics (`ResilienceMeter`) and Tracing (`ResilienceActivitySource`) capture duration, retry counts, and tenant metadata.
