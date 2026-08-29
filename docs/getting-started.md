# Getting Started with EricksonLopez.Resilience

## Overview

`EricksonLopez.Resilience` provides an enterprise-grade architectural resilience framework for modern .NET systems (.NET 8, .NET 9, .NET 10), engineered strictly following Clean Architecture and Domain-Driven Design (DDD) principles.

The foundational design invariant is:
> **Polly is an internal infrastructure execution engine; the EricksonLopez application and domain layers depend exclusively on first-party abstractions.**

## Key Capabilities

- **Zero-Coupling Abstractions**: Clean separation between policy declarations (`IResiliencePipelineBuilder`), execution interfaces (`IResilienceExecutor`, `IResiliencePipeline`), and the underlying engine (`Polly.Core`).
- **Rich Result Integration**: First-class support for `EricksonLopez.Result.Result<T>`, evaluating `ErrorRetryability` (`Transient` vs. `Permanent`) and `ErrorType` categories (`Unavailable`, `Infrastructure` vs. `Validation`, `Domain`, `Conflict`).
- **Zero-Allocation Pipeline Behaviors**: Struct-continuation Mediator behavior integrating directly with `EricksonLopez.Mediator`.
- **Ecosystem Integrity**: Built-in transactional safety (`Resilience -> Transaction`), idempotency key preservation (`Resilience -> Idempotency`), and concurrency retry patterns.
- **Native AOT & Trimming**: 100% Native AOT compatible (`EnableTrimAnalyzer=true`, `TreatWarningsAsErrors=true`) with zero runtime reflection.
- **OpenTelemetry Instrumentation**: Semantic metrics (`resilience.execution.duration`, `resilience.retry.attempts`) and distributed tracing spans.

---

## Installation

Install the required packages depending on your layer:

```bash
# Core Abstractions (Application Layer - L0 Foundation)
dotnet add package EricksonLopez.Resilience.Abstractions

# Resilience Core & Classifiers (Application / Infrastructure - L2 Core)
dotnet add package EricksonLopez.Resilience

# Polly Infrastructure Execution Adapter (Infrastructure Layer - L4)
dotnet add package EricksonLopez.Resilience.Polly

# Dependency Injection Extensions (Host / Composition Root - L3)
dotnet add package EricksonLopez.Resilience.DependencyInjection

# Mediator Pipeline Behavior (Optional - Application Layer - L3)
dotnet add package EricksonLopez.Resilience.Mediator

# OpenTelemetry Instrumentation (Optional - Host / Observability - L3)
dotnet add package EricksonLopez.Resilience.OpenTelemetry

# ASP.NET Core & HTTP Extensions (Optional - Presentation Layer - L3)
dotnet add package EricksonLopez.Resilience.AspNetCore
```

---

## Quickstart

### 1. Register Resilience Services

In your application entry point (`Program.cs` or Service Configuration):

```csharp
using System;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Register EricksonLopez.Resilience with Polly execution adapter
services.AddEricksonLopezResilience();

// Register a typed resilience policy
services.AddResiliencePolicy<PaymentGatewayResiliencePolicy>();

// Register an inline named resilience policy
services.AddResiliencePolicy("inventory-service", policyBuilder =>
{
    // Note: AddStandardResilience() already includes a 30-second timeout.
    // Calling AddTimeout() here adds a second outer timeout. Ensure both durations
    // are intentional; typically you would use AddStandardResilience() alone
    // or configure a custom policy without AddStandardResilience().
    policyBuilder
        .AddStandardResilience()
        .AddTimeout(TimeSpan.FromSeconds(3));
});
```

### 2. Define a Strongly-Typed Policy

```csharp
using System;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Policies;

public sealed class PaymentGatewayResiliencePolicy : ResiliencePolicy
{
    public override string Name => "payment-gateway";

    public override void Configure(IResiliencePipelineBuilder builder)
    {
        builder
            .AddRetry(opt =>
            {
                opt.MaxRetryAttempts = 3;
                opt.Delay = TimeSpan.FromMilliseconds(200);
                opt.BackoffType = BackoffType.ExponentialWithJitter;
            })
            .AddCircuitBreaker(opt =>
            {
                opt.FailureRatio = 0.5;
                opt.MinimumThroughput = 10;
                opt.SamplingDuration = TimeSpan.FromSeconds(30);
                opt.BreakDuration = TimeSpan.FromSeconds(15);
            })
            .AddTimeout(TimeSpan.FromSeconds(5));
    }
}
```

### 3. Execute Operations Resiliently

Inject `IResilienceExecutor` into your application service:

```csharp
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience;
using EricksonLopez.Result;

public sealed class PaymentService(IResilienceExecutor executor, IPaymentGatewayClient gatewayClient)
{
    public async ValueTask<Result<PaymentConfirmation>> ProcessPaymentAsync(
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        // Use the CancellationToken overload for simple operations.
        // For richer telemetry (CorrelationId, TenantId), pass an explicit ResilienceContext.
        return await executor.ExecuteAsync(
            "payment-gateway",
            async ct =>
            {
                // Operation automatically retried on transient errors/exceptions
                return await gatewayClient.ChargeCardAsync(request, ct);
            },
            cancellationToken);
    }
}
```
