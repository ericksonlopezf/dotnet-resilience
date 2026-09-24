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
11. [Recipe 11: RateLimitRejectedException Handling & Retry-After Extraction](#recipe-11-ratelimitrejectedexception-handling--retry-after-extraction)
12. [Recipe 12: BackoffType Algorithm Variants](#recipe-12-backofftype-algorithm-variants)
13. [Recipe 13: RateLimiterType Variants](#recipe-13-ratelimitertype-variants)
14. [Recipe 14: Immutable ResilienceContext Builder Chain](#recipe-14-immutable-resiliencecontext-builder-chain)
15. [Recipe 15: Advanced Fallback — OnFallback Callback and Conditional Triggers](#recipe-15-advanced-fallback--onfallback-callback-and-conditional-triggers)
16. [Recipe 16: Direct Options-Object Overloads — AddRateLimiter and AddHedging](#recipe-16-direct-options-object-overloads--addratelimiter-and-addhedging)
17. [Recipe 17: Dynamic Policy Configuration via IServiceProvider](#recipe-17-dynamic-policy-configuration-via-iserviceprovider)
18. [Recipe 18: Privacy-Compliant Tracing with ExceptionSanitizer](#recipe-18-privacy-compliant-tracing-with-exceptionsanitizer)
19. [Recipe 19: Speculative Hedging across Alternate Replicas with HedgedActionGenerator](#recipe-19-speculative-hedging-across-alternate-replicas-with-hedgedactiongenerator)

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

---

## Recipe 11: RateLimitRejectedException Handling & Retry-After Extraction
- **Problem**: When a downstream microservice or API gateway throttles incoming traffic, standard exception handlers drop the response or throw generic 500 errors without informing clients when they may safely retry.
- **Solution**: Catch `RateLimitRejectedException` and inspect its `RetryAfter` nullable `TimeSpan` property to populate standard HTTP 429 `Retry-After` headers.
- **Code**:
```csharp
try
{
    await executor.ExecuteAsync("strict-rate-limiter", async ct =>
    {
        return await client.GetAsync("/api/data", ct);
    });
}
catch (RateLimitRejectedException ex)
{
    var retryAfterSeconds = ex.RetryAfter?.TotalSeconds ?? 1.0;
    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString("F0");
}
```
- **Best Practices**: Always configure `QueueLimit = 0` on latency-sensitive public endpoints to fail-fast and propagate HTTP 429 rather than queueing requests into high tail latencies.
- **Common Pitfalls**: Ignoring `RetryAfter` and retrying immediately, causing sustained self-inflicted rejection loops.

---

## Recipe 12: BackoffType Algorithm Variants
- **Problem**: Using uniform or constant delay in distributed microservice retries causes correlated thundering herd storms on recovering downstream dependencies.
- **Solution**: Select the appropriate `BackoffType` algorithm (`Constant`, `Linear`, `Exponential`, or `ExponentialWithJitter`) matching your traffic topology and SLA constraints.
- **Code**:
```csharp
services.AddResiliencePolicy("jittered-payment-retry", builder =>
{
    builder.AddRetry(opt =>
    {
        opt.MaxRetryAttempts = 3;
        opt.Delay = TimeSpan.FromMilliseconds(50);
        opt.BackoffType = BackoffType.ExponentialWithJitter;
        opt.MaxDelay = TimeSpan.FromSeconds(2);
    });
});
```
- **Best Practices**: Default to `ExponentialWithJitter` for all inter-service HTTP and messaging communications in distributed architectures.
- **Common Pitfalls**: Using `Constant` backoff under high concurrency, which synchronizes retry intervals across hundreds of client instances.

---

## Recipe 13: RateLimiterType Variants
- **Problem**: Applying an ill-suited rate limiting algorithm leads to unexpected request bursts or starvation.
- **Solution**: Tailor `RateLimiterType` to the exact resource constraint:
  - `SlidingWindow`: Eliminates burst vulnerability at fixed window boundaries.
  - `FixedWindow`: Lightweight counting for coarse-grained hourly/daily tier quotas.
  - `TokenBucket`: Allows controlled bursts while enforcing an average fill rate.
  - `Concurrency`: Enforces strict maximum parallel executions (connection pools, CPU-intensive tasks).
- **Code**:
```csharp
services.AddResiliencePolicy("db-pool-concurrency", builder =>
{
    builder.AddRateLimiter(opt =>
    {
        opt.LimiterType = RateLimiterType.Concurrency;
        opt.PermitLimit = 10;
        opt.QueueLimit = 20;
    });
});
```
- **Best Practices**: Use `RateLimiterType.Concurrency` for database connection pools, file decoders, or memory-heavy image processing.
- **Common Pitfalls**: Using `FixedWindow` on high-traffic APIs where 100% of the quota can be consumed in the last second of window N and the first second of window N+1.

---

## Recipe 14: Immutable ResilienceContext Builder Chain
- **Problem**: Mutating shared execution contexts across retries causes concurrency race conditions and dirty telemetry state.
- **Solution**: Leverage `ResilienceContext` immutable builder methods (`WithOperationName`, `WithCorrelationId`, `WithTenantId`, `WithAttemptNumber`, `SetProperty`) which return fresh isolated instances.
- **Code**:
```csharp
var context = ResilienceContext.Create("OrderProcessing")
    .WithCorrelationId(Guid.NewGuid().ToString())
    .WithTenantId("TENANT-ACME-CORP")
    .WithAttemptNumber(1)
    .SetProperty("OrderId", "ORD-9912");

await executor.ExecuteAsync("payment-policy", async ctx =>
{
    var tenant = ctx.TenantId;
    var correlation = ctx.CorrelationId;
    return await ProcessPaymentAsync(tenant, correlation, ctx.CancellationToken);
}, context);
```
- **Best Practices**: Pass the enriched `ResilienceContext` down the call chain to correlate retries, circuit breaker events, and OpenTelemetry spans.
- **Common Pitfalls**: Storing mutable domain entities inside `ResilienceContext` properties without thread-safety synchronization.

---

## Recipe 15: Advanced Fallback — OnFallback Callback and Conditional Triggers
- **Problem**: Default fallback policies blindly catch all exceptions, masking critical business assertion errors or authentication failures.
- **Solution**: Configure `ShouldHandleException` and `ShouldHandleResult` predicates alongside an asynchronous `OnFallback` callback in `FallbackStrategyOptions<TResult>`.
- **Code**:
```csharp
var pipeline = new ResiliencePipelineBuilder<CatalogResponse>("catalog-fallback")
    .AddFallback(opt =>
    {
        opt.ShouldHandleException = ex => ex is HttpRequestException or TimeoutException;
        opt.ShouldHandleResult = res => res == null || res.IsDegraded;
        opt.OnFallback = ctx =>
        {
            logger.LogWarning("Fallback activated for Operation: {Op}", ctx.OperationName);
            return ValueTask.CompletedTask;
        };
        opt.FallbackAction = ctx => ValueTask.FromResult(CatalogResponse.CachedFallback);
    })
    .Build();
```
- **Best Practices**: Always log or record metrics inside `OnFallback` to retain visibility into degraded customer experiences.
- **Common Pitfalls**: Returning stale fallback data for operations that mutate state (e.g. fund transfers, order creations).

---

## Recipe 16: Direct Options-Object Overloads — AddRateLimiter and AddHedging
- **Problem**: In microservice architectures with dozens of pipelines, allocating and re-configuring identical options lambda delegates causes boilerplate duplication.
- **Solution**: Pre-construct reusable `RateLimiterStrategyOptions` and `HedgingStrategyOptions` instances and pass them directly to the `AddRateLimiter(options)` and `AddHedging(options)` overloads.
- **Code**:
```csharp
var sharedLimiter = new RateLimiterStrategyOptions
{
    Name = "SharedPublicApiLimiter",
    PermitLimit = 100,
    Window = TimeSpan.FromSeconds(1),
    LimiterType = RateLimiterType.TokenBucket
};

services.AddResiliencePolicy("orders-api", b => b.AddRateLimiter(sharedLimiter));
services.AddResiliencePolicy("customers-api", b => b.AddRateLimiter(sharedLimiter));
```
- **Best Practices**: Validate options objects once during application composition root initialization.
- **Common Pitfalls**: Mutating options properties after registering them with builders—options instances are referenced directly.

---

## Recipe 17: Dynamic Policy Configuration via IServiceProvider
- **Problem**: Hardcoding timeout or retry values prevents adapting to runtime environment configurations or DI-injected settings.
- **Solution**: Use the `AddResiliencePolicy(name, (builder, sp) => { ... })` overload to resolve registered dependencies directly from `IServiceProvider`.
- **Code**:
```csharp
services.AddResiliencePolicy("external-partner-policy", (builder, sp) =>
{
    var config = sp.GetRequiredService<IOptions<PartnerServiceOptions>>().Value;
    builder
        .AddTimeout(config.RequestTimeout)
        .AddRetry(opt =>
        {
            opt.MaxRetryAttempts = config.MaxRetries;
            opt.Delay = config.RetryDelay;
            opt.BackoffType = BackoffType.ExponentialWithJitter;
        });
});
```
- **Best Practices**: Resolve singleton configuration classes (`IOptions<T>`) rather than scoped entities during pipeline definition.
- **Common Pitfalls**: Attempting to resolve scoped database contexts inside singleton pipeline registration delegates.

---

## Recipe 18: Privacy-Compliant Tracing with ExceptionSanitizer
- **Problem**: Distributed trace spans exported to cloud observability backends (Datadog, Dynatrace, New Relic) may inadvertently expose sensitive customer PII or authentication bearer tokens in exception messages.
- **Solution**: Register a custom delegate on `ResilienceActivitySource.ExceptionSanitizer` to redact sensitive patterns before exceptions are recorded on OpenTelemetry `Activity` spans.
- **Code**:
```csharp
ResilienceActivitySource.ExceptionSanitizer = ex =>
{
    var sanitizedMessage = Regex.Replace(ex.Message, @"Bearer\s+[A-Za-z0-9\-\._~\+\/]+=*", "Bearer [REDACTED]");
    return (sanitizedMessage, ex.StackTrace ?? string.Empty);
};
```
- **Best Practices**: Configure `ExceptionSanitizer` during application boot in `Program.cs` before any resilient operations execute.
- **Common Pitfalls**: Leaving `ExceptionSanitizer` unconfigured in systems subject to GDPR, HIPAA, or PCI-DSS compliance audits.

---

## Recipe 19: Speculative Hedging across Alternate Replicas with HedgedActionGenerator
- **Problem**: Retrying or hedging against the same degraded server replica fails to circumvent host-level lock contention, garbage collection pauses, or network link degradation.
- **Solution**: Configure `HedgedActionGenerator` on `HedgingStrategyOptions<TResult>` to dynamically target secondary availability zones or read-only replicas on speculative hedging attempts.
- **Code**:
```csharp
var pipeline = new ResiliencePipelineBuilder<OrderSummary>("active-active-hedging")
    .AddHedging(opt =>
    {
        opt.MaxHedgedAttempts = 1;
        opt.Delay = TimeSpan.FromMilliseconds(50);
        opt.HedgedActionGenerator = ctx =>
        {
            return async () =>
            {
                return await secondaryReplicaClient.GetOrderAsync(orderId);
            };
        };
    })
    .Build();
```
- **Best Practices**: Hedging MUST ONLY be used for strictly idempotent, side-effect-free read operations.
- **Common Pitfalls**: Applying speculative hedging to payment authorizations or inventory decrements, causing duplicate mutations.

