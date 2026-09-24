# ADR-004: Transactional Boundaries and Idempotency Key Preservation

## Status
Accepted

## Date
2026-09-04

## Context
When an operation fails due to transient database deadlocks, optimistic concurrency conflicts, or network drops, executing retries without clean boundary management leads to data corruption:
- Reusing an existing transaction or `DbContext` with dirty tracking state causes subsequent commits to fail or write invalid partial states.
- Altering idempotency keys between retry attempts causes the receiving system to process the same logical command multiple times.

## Decision
1. **Clean Unit of Work Per Attempt**:
   - Each execution attempt must instantiate and delimit its own fresh transaction/Unit of Work *inside* the execution delegate passed to `IResilienceExecutor`.
   - On transient failure, the dirty transaction is rolled back and discarded before the next attempt starts.
2. **Idempotency Key Preservation**:
   - The same `IdempotencyKey` must be preserved across all retry attempts within the `ResilienceContext`.
3. **Optimistic Concurrency State Reload**:
   - In concurrency conflict scenarios, each attempt reloads the latest aggregate version from the database before applying domain mutations.

## Consequences
### Positive
- Guarantees ACID transactional integrity during transient database failures.
- Ensures absolute idempotency across distributed networks.
- Prevents dirty entity tracking and phantom writes in ORMs.

### Negative / Tradeoffs
- Requires developers to structure transactional logic inside resilient delegates rather than wrapping retry policies inside ambient transactions.
