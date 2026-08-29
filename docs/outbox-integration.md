# Outbox Pattern & Resilience

## Overview

The Transactional Outbox pattern guarantees eventual consistency and at-least-once messaging by persisting domain events into an `OutboxMessages` table in the same database transaction as the aggregate state changes.

When retrying operations with resilience:
> **The Outbox message persistence must be part of the atomic transaction executed within each retry attempt.**

---

## Architecture Flow

```mermaid
sequenceDiagram
    autonumber
    participant Client
    participant Resilience as Resilience Retry Scope
    participant Service as Order Application Service
    participant Tx as Atomic DB Transaction
    participant DB as PostgreSQL Database

    Client->>Resilience: Submit Order
    Resilience->>Service: Execute Attempt 1
    Service->>Tx: Begin Transaction
    Service->>Tx: Insert Order Aggregate
    Service->>Tx: Insert OutboxMessage (OrderCreatedDomainEvent)
    Tx->>DB: Commit
    DB-->>Tx: Deadlock / Transient Lock Timeout
    Tx-->>Service: Rollback Transaction (Nothing Persisted)
    Service-->>Resilience: IOException / DbException

    Note over Resilience: Exponential Jitter Delay

    Resilience->>Service: Execute Attempt 2
    Service->>Tx: Begin FRESH Transaction
    Service->>Tx: Insert Order Aggregate
    Service->>Tx: Insert OutboxMessage (OrderCreatedDomainEvent)
    Tx->>DB: Commit
    DB-->>Tx: Success!
    Tx-->>Service: Transaction Committed
    Service-->>Resilience: Result.Success
    Resilience-->>Client: Result.Success
```

---

## Atomicity Guarantee

Because both the domain state and the outbox message rollback together on failure:
- **No Orphan Messages**: Outbox messages are never published if the order creation fails.
- **No Duplicate Outbox Messages**: If attempt 1 fails before commit, attempt 2 creates exactly 1 order and 1 outbox message.
