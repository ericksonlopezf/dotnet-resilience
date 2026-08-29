# Cookbook: Production Recipes Collection

This cookbook provides ready-to-use, battle-tested resilience recipes for enterprise .NET 10 systems using `EricksonLopez.Resilience`.

---

## Index of Recipes
1. [Recipe 1: Automatic Retry of Transient Errors with Result<T>](#recipe-1-automatic-retry-of-transient-errors-with-resultt)
2. [Recipe 2: Cascading Failure Prevention with Circuit Breaker & Fallback](#recipe-2-cascading-failure-prevention-with-circuit-breaker--fallback)
3. [Recipe 3: Transactional Boundary & Idempotency across Retries](#recipe-3-transactional-boundary--idempotency-across-retries)
4. [Recipe 4: Resilient Pipeline Behavior with Mediator](#recipe-4-resilient-pipeline-behavior-with-mediator)
5. [Recipe 5: Resilient HttpClient with DelegatingHandler & OpenTelemetry](#recipe-5-resilient-httpclient-with-delegatinghandler--opentelemetry)
6. [Recipe 6: Reusable Strongly-Typed Resilience Policies](#recipe-6-reusable-strongly-typed-resilience-policies)
7. [Recipe 7: Traffic Spike Protection with Sliding Window Rate Limiter](#recipe-7-traffic-spike-protection-with-sliding-window-rate-limiter)
8. [Recipe 8: Strongly-Typed Fallback with Custom Value Generation](#recipe-8-strongly-typed-fallback-with-custom-value-generation)
9. [Recipe 9: Zero-Code Dynamic Policy Configuration from IConfiguration](#recipe-9-zero-code-dynamic-policy-configuration-from-iconfiguration)
10. [Recipe 10: Speculative Parallel Hedging for Idempotent Reads](#recipe-10-speculative-parallel-hedging-for-idempotent-reads)

---

## Recipe 1: Automatic Retry of Transient Errors with Result<T>
- **Problem**: Exception-only retry strategies fail to handle transient errors represented as `Result.Failure(Error.Unavailable)`.
- **Solution**: Use `AddResultRetry()` to evaluate both exceptions and `Result<T>` instances.
- **Code**:
```csharp
services.AddResiliencePolicy("payment-policy", builder =>
{
    builder.AddResultRetry(opt =>
    {
        opt.MaxRetryAttempts = 3;
        opt.Delay = TimeSpan.FromMilliseconds(100);
        opt.BackoffType = BackoffType.ExponentialWithJitter;
    });
});
```
- **Best Practices**: Ensure domain errors (`Error.Validation`, `Error.Domain`) use `ErrorRetryability.Permanent` so they are not retried.

---

## Recipe 2: Cascading Failure Prevention with Circuit Breaker & Fallback
- **Problem**: Failing downstream dependencies tie up threads and connection pools.
- **Solution**: Configure `AddCircuitBreaker()` and intercept `CircuitBrokenException` to return cached fallback data.
- **Code**:
```csharp
services.AddResiliencePolicy("inventory-circuit", builder =>
{
    builder.AddCircuitBreaker(opt =>
    {
        opt.FailureRatio = 0.5;
        opt.MinimumThroughput = 10;
        opt.SamplingDuration = TimeSpan.FromSeconds(30);
        opt.BreakDuration = TimeSpan.FromSeconds(15);
    });
});
```

---

## Recipe 3: Transactional Boundary & Idempotency across Retries
- **Problem**: Retrying on a failed, dirty `DbContext` context corrupts entity tracking.
- **Solution**: Open a fresh Unit of Work / Transaction inside the execution delegate, preserving the idempotency key across attempts.
- **Code**:
```csharp
await executor.ExecuteAsync("database-policy", async ctx =>
{
    using var uow = uowFactory.Create();
    await uow.ProcessIdempotentAsync(idempotencyKey, ctx.CancellationToken);
    await uow.CommitAsync(ctx.CancellationToken);
    return Result.Success();
}, context);
```

---

## Recipe 4: Resilient Pipeline Behavior with Mediator
- **Problem**: Calling resilience policies manually in handlers leads to boilerplate duplication.
- **Solution**: Implement `IResilientRequest` on commands and register `AddResiliencePipelineBehavior()`.

---

## Recipe 5: Resilient HttpClient with DelegatingHandler & OpenTelemetry
- **Problem**: HTTP clients need unified timeout, retry, circuit breaker, and tracing.
- **Solution**: Use `builder.AddStandardResilienceHandler()` or `builder.AddResiliencePolicy()`.

---

## Recipe 6: Reusable Strongly-Typed Resilience Policies
- **Problem**: Magic string policy names risk typos.
- **Solution**: Derive from `ResiliencePolicy` and register via `services.AddResiliencePolicy<TPolicy>()`.

---

## Recipe 7: Traffic Spike Protection with Sliding Window Rate Limiter
- **Problem**: Traffic bursts overwhelm memory and downstream databases.
- **Solution**: Configure `AddRateLimiter(opt => opt.LimiterType = RateLimiterType.SlidingWindow)`.

---

## Recipe 8: Strongly-Typed Fallback with Custom Value Generation
- **Problem**: Degraded read responses must be returned without raising exceptions.
- **Solution**: Use `ResiliencePipelineBuilder<TResult>.AddFallback()`.

---

## Recipe 9: Zero-Code Dynamic Policy Configuration from IConfiguration
- **Problem**: Ops teams need to adjust retry counts and timeouts without code redeployment.
- **Solution**: Use `services.AddResiliencePolicyFromConfiguration(policyName, configurationSection)`.

---

## Recipe 10: Speculative Parallel Hedging for Idempotent Reads
- **Problem**: Tail latency spikes across cloud replicas hurt P99 SLA.
- **Solution**: Use `ResiliencePipelineBuilder<TResult>.AddHedging()` for read-only queries.
