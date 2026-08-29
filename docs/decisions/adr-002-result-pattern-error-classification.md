# ADR-002: Native Result Pattern and Error Classification

## Status
**Accepted**

## Context
Traditional resilience libraries evaluate only exceptions (`System.Exception`). In high-performance, domain-driven architectures, many operational failures (e.g., downstream service unavailable, transient gateway timeout, rate limit exceeded) are modeled using functional results (`Result<T>`) rather than thrown exceptions to avoid expensive stack trace generation.

## Decision
We introduce first-class Result Pattern evaluation via `IResultRetryClassifier` and `ResultRetryClassifier`:
1. **Result Evaluation**: Inspects `IResultOutcome.Error`.
2. **Error Retryability**:
   - `ErrorRetryability.Transient`: Automatically triggers retry with exponential jitter backoff.
   - `ErrorRetryability.Permanent` or `NotApplicable`: Avoids retries, returning failure immediately.
3. **Category Fallbacks**:
   - `ErrorType.Unavailable` and `ErrorType.Infrastructure` are classified as retryable by default.
   - `ErrorType.Validation`, `ErrorType.Domain`, `ErrorType.Unauthorized`, `ErrorType.Forbidden`, `ErrorType.NotFound`, and `ErrorType.Conflict` are strictly non-retryable.
4. **Convenience Extensions**: `builder.AddResultRetry()` seamlessly configures both exception-based and Result-based retries.

## Consequences
### Positive
- Unified resilience model handling both runtime exceptions and functional domain failures (`Result<T>`).
- Prevents wasteful retries on business rule violations or invalid input payloads.
- Significant CPU and allocation savings by avoiding exception-throwing on predictable transient outages.

### Negative / Tradeoffs
- Requires application results to implement or be adaptable to `IResultOutcome` / `Result<T>`.
