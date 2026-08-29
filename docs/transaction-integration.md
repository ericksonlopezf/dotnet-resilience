# Transaction Boundaries & Resilience

## Overview

A critical architectural invariant when integrating resilience with transactional systems is the boundary hierarchy:

> **Resilience must enclose the Transaction boundary (`Resilience -> Transaction`).**

Never place the resilience retry loop inside an open database transaction.

```mermaid
flowchart TD
    subgraph WRONG ["❌ INCORRECT: Transaction wraps Resilience"]
        TxOuter[Open DbTransaction]
        RetryInner[Resilience Retry Loop]
        DirtyState[Attempt 1 Fails -> DbConnection Dirty / Aborted]
        TxOuter --> RetryInner --> DirtyState
    end

    subgraph CORRECT ["✅ CORRECT: Resilience wraps Transaction"]
        ResilientOuter[Resilience Retry Loop]
        TxAttempt1["Attempt 1: Open Tx -> Fail -> Rollback & Dispose Tx"]
        TxAttempt2["Attempt 2: Open FRESH Tx -> Execute -> Commit & Dispose Tx"]
        ResilientOuter --> TxAttempt1
        ResilientOuter --> TxAttempt2
    end
```

---

## Why Inner Transactions are Mandatory

1. **Deadlock / Poisoned State**: Once a PostgreSQL / SQL Server transaction experiences a conflict or socket error, the transaction state becomes `ABORTED`. Attempting queries on an aborted transaction throws `25P02: current transaction is aborted, commands ignored until end of transaction block`.
2. **Resource Leaks**: Holding database locks open while sleeping in a retry backoff delay starves other connections.
3. **Clean Isolation**: Every retry attempt must instantiate and commit/rollback a completely new `IUnitOfWork` or `IDbTransaction`.

---

## Correct Implementation Pattern

```csharp
public async ValueTask<Result<Guid>> CreateInvoiceAsync(CreateInvoiceCommand cmd, CancellationToken ct)
{
    // Resilience wraps the entire transaction scope
    return await _resilienceExecutor.ExecuteAsync(
        "transaction-policy",
        async ctx =>
        {
            // Fresh UnitOfWork instantiated per attempt
            await using var uow = await _unitOfWorkFactory.CreateAsync(ctx.CancellationToken);
            
            var invoice = Invoice.Create(cmd.CustomerId, cmd.Amount);
            await uow.Invoices.AddAsync(invoice, ctx.CancellationToken);
            
            await uow.CommitAsync(ctx.CancellationToken);
            return Result<Guid>.Success(invoice.Id);
        },
        ct);
}
```
