# Timeout Strategy

## Overview

The timeout strategy guarantees that callers never hang indefinitely waiting for an unresponsive downstream dependency.

## Invariants & Best Practices

- **Timeout is not user cancellation**: A timeout indicates that an SLA was violated. The framework throws `ResilienceTimeoutException` (or returns an unavailable `Result<T>`).
- **Cooperative Cancellation**: Downstream operations must accept and observe the `CancellationToken` provided in `ResilienceContext` to release network sockets and database connections promptly.

---

## Configuration

```csharp
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Options;

// Simple timeout helper
builder.AddTimeout(TimeSpan.FromSeconds(5));

// Detailed configuration
builder.AddTimeout(options =>
{
    options.Timeout = TimeSpan.FromSeconds(5);
    options.Name = "PaymentGatewayTimeout";

    options.OnTimeout = timeoutContext =>
    {
        Console.WriteLine($"Policy '{timeoutContext.ResilienceContext?.PolicyName}' timed out after {timeoutContext.Timeout.TotalMilliseconds}ms");
        return ValueTask.CompletedTask;
    };
});
```

---

## Exception Hierarchy

When a timeout fires, Polly's internal `TimeoutRejectedException` is caught and translated to `EricksonLopez.Resilience.Exceptions.ResilienceTimeoutException`:

```csharp
try
{
    await executor.ExecuteAsync("payment-policy", async ctx => await httpClient.GetAsync(url, ctx.CancellationToken));
}
catch (ResilienceTimeoutException ex)
{
    Console.WriteLine($"Operation timed out: {ex.Timeout.TotalSeconds}s for policy '{ex.PolicyName}'");
}
```
