# Result Pattern Integration

## Overview

In the EricksonLopez ecosystem, exceptions are strictly prohibited for domain and application control flow. Errors are modeled as first-class domain values using `EricksonLopez.Result.Result<T>` and `EricksonLopez.Result.Error`.

`EricksonLopez.Resilience` integrates natively with `Result<T>`:
1. **Explicit Error Classification**: Evaluates `Error.Retryability` (`Transient` vs. `Permanent`).
2. **Category Classification**: Inspects `ErrorType` (`Unavailable`, `Infrastructure` are retried; `Validation`, `Domain`, `Conflict`, `Unauthorized`, `Forbidden`, `NotFound` are not retried).
3. **Exception Transparency**: Classifies transient network, socket, and HTTP 5xx errors while distinguishing user cancellation (`OperationCanceledException`) from timeout SLA violations.

---

## Result Retry Decision Table

| Outcome | Classification | Action |
|---|---|---|
| `Result<T>.Success(value)` | Success | Return value immediately. |
| `Result<T>.Failure(Error)` with `ErrorRetryability.Transient` | Transient Error | Retry with backoff. |
| `Result<T>.Failure(Error)` with `ErrorRetryability.Permanent` | Permanent Error | Return Failure immediately. |
| `Result<T>.Failure(Error)` with `ErrorType.Unavailable` / `Infrastructure` | Transient Infrastructure | Retry with backoff. |
| `Result<T>.Failure(Error)` with `ErrorType.Validation` / `Domain` / `Conflict` / `Unauthorized` / `Forbidden` / `NotFound` | Business Rule / Deterministic Client Error | Return Failure immediately without retry. |
| `TimeoutException` / `ResilienceTimeoutException` | Transient SLA Violation | Retry with backoff. |
| `OperationCanceledException` (Caller cancellation) | Client Intentional Cancellation | Abort immediately without retrying. |

---

## Code Example

```csharp
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience;
using EricksonLopez.Result;

public sealed class CustomerService
{
    private readonly IResilienceExecutor _executor;
    private readonly ICustomerApiClient _apiClient;

    public CustomerService(IResilienceExecutor executor, ICustomerApiClient apiClient)
    {
        _executor = executor;
        _apiClient = apiClient;
    }

    public async ValueTask<Result<CustomerDto>> GetCustomerAsync(Guid customerId, CancellationToken ct)
    {
        return await _executor.ExecuteAsync(
            "customer-api",
            async ctx =>
            {
                // If this returns Result.Failure(Error.Unavailable(...)), the executor retries automatically.
                // If it returns Result.Failure(Error.NotFound(...)), the executor returns failure immediately.
                return await _apiClient.FetchCustomerAsync(customerId, ctx.CancellationToken);
            },
            ct);
    }
}
```
