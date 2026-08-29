# Level 04: Advanced Integration — Mediator & Transaction Boundaries

## 1. Clean Mediator Pipeline Interception
In CQRS architectures with `EricksonLopez.Mediator`, commands declare their resilience requirements using `IResilientRequest`. The open-generic `ResiliencePipelineBehavior<TRequest, TResponse>` automatically intercepts execution without reflection:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Mediator.Extensions;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEricksonLopezResilience();
services.AddResiliencePolicy("order-submission-policy", builder =>
{
    builder
        .AddTimeout(TimeSpan.FromSeconds(5))
        .AddResultRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(100);
            opt.BackoffType = Options.BackoffType.ExponentialWithJitter;
        });
});

// Register Mediator resilience behavior
services.AddResiliencePipelineBehavior();
```

---

## 2. Command Declaring `IResilientRequest`
Commands specify their policy name through a clean interface:

```csharp
public sealed record SubmitOrderCommand(string CustomerId, decimal Total, string IdempotencyKey)
    : ICommand<Result<Guid>>, IResilientRequest
{
    public string ResiliencePolicy => "order-submission-policy";
}
```

---

## 3. Transactional Boundaries & Idempotency Safety
When retrying database operations:
- **Clean Unit of Work**: A fresh database transaction must be initiated inside the delegate on each retry attempt.
- **Idempotency Key Preservation**: The same `IdempotencyKey` travels across all retry attempts, ensuring downstream gateways or event outboxes never duplicate transactions.

```csharp
public sealed class SubmitOrderCommandHandler
{
    public async ValueTask<Result<Guid>> Handle(SubmitOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Delimit transactional Unit of Work per attempt
        using var unitOfWork = _unitOfWorkFactory.Create();
        
        // 2. Validate idempotency record
        if (await _idempotencyStore.TryLockKeyAsync(request.IdempotencyKey, cancellationToken))
        {
            // Process order...
            await unitOfWork.CommitAsync(cancellationToken);
            return Result<Guid>.Success(orderId);
        }

        return Result<Guid>.Failure(Error.Conflict("Order.Duplicate", "Order already processed"));
    }
}
```
