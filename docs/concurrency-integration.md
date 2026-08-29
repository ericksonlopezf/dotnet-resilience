# Optimistic Concurrency & Resilience

## Overview

In high-concurrency Domain-Driven Design architectures, entities maintain a `Version` or `RowVersion` to detect concurrent writes.

When two threads update the same aggregate simultaneously, one commits while the other fails with a `ConcurrencyConflictException`.

## The Invariant: Re-read Before Retry

> **Retrying a concurrency conflict without reloading fresh aggregate state is a fatal bug.**

If an application blindly re-attempts the database `UPDATE` with stale in-memory data, it will either fail indefinitely or overwrite changes made by the competing transaction.

---

## The Correct Concurrency Retry Pattern

```mermaid
sequenceDiagram
    autonumber
    participant RetryLoop as Resilience Pipeline
    participant Service as Application Service
    participant Repo as Aggregate Repository / DB

    Note over RetryLoop, Repo: Attempt 1
    RetryLoop->>Service: Execute Attempt 1
    Service->>Repo: 1. Read Latest Aggregate (Version = 1)
    Service->>Service: 2. Apply Domain Mutation
    Service->>Repo: 3. Commit with Expected Version = 1
    Repo-->>Service: Concurrency Conflict! (Version was bumped to 2 by another thread)
    Service-->>RetryLoop: ConcurrencyConflictException

    Note over RetryLoop, Repo: Attempt 2 (Retry)
    RetryLoop->>Service: Execute Attempt 2
    Service->>Repo: 1. Re-read Fresh Aggregate (Version = 2)
    Service->>Service: 2. Re-apply Domain Mutation
    Service->>Repo: 3. Commit with Expected Version = 2
    Repo-->>Service: Success (Version = 3)
    Service-->>RetryLoop: Result.Success
```

---

## Code Example

```csharp
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience;
using EricksonLopez.Result;

public sealed class AccountService(IResilienceExecutor executor, IAccountRepository accountRepository)
{
    public async ValueTask<Result<bool>> DebitAccountAsync(Guid accountId, decimal amount, CancellationToken ct)
    {
        return await executor.ExecuteAsync(
            "account-debit-policy",
            async ctx =>
            {
                // 1. MUST re-read the fresh aggregate version inside the retry delegate
                var account = await accountRepository.GetByIdAsync(accountId, ctx.CancellationToken);
                if (account is null)
                {
                    return Result<bool>.Failure(Error.NotFound("Account.NotFound", "Account does not exist."));
                }

                // 2. Re-apply business logic on the new state
                var domainResult = account.Debit(amount);
                if (domainResult.IsFailure)
                {
                    return domainResult; // Business validation failure; do not retry
                }

                // 3. Commit to database with version check
                return await accountRepository.UpdateAsync(account, ctx.CancellationToken);
            },
            ct);
    }
}
```
