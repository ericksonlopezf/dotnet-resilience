# Resilient HttpClient Integration

## Overview

When communicating with external HTTP endpoints, transient network drops, socket resets, and HTTP 5xx responses (503 Service Unavailable, 502 Bad Gateway, 504 Gateway Timeout, 429 Too Many Requests) must be handled gracefully.

`EricksonLopez.Resilience.AspNetCore` provides `ResilienceDelegatingHandler` and fluent `IHttpClientBuilder` extensions to apply named resilience policies directly to named and typed HTTP clients.

---

## Configuration

```csharp
using System;
using System.Net.Http;
using System.Net.Sockets;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// 1. Register Resilience Policy
services.AddEricksonLopezResilience();
services.AddResiliencePolicy("stripe-gateway-policy", policyBuilder =>
{
    policyBuilder
        .AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(250);
            opt.BackoffType = BackoffType.ExponentialWithJitter;
            opt.ShouldHandleException = ex => ex is HttpRequestException or SocketException;
        })
        .AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.SamplingDuration = TimeSpan.FromSeconds(30);
            opt.BreakDuration = TimeSpan.FromSeconds(10);
        })
        .AddTimeout(TimeSpan.FromSeconds(5));
});

// 2. Attach Policy to Typed HttpClient
services.AddHttpClient("StripeClient", client =>
{
    client.BaseAddress = new Uri("https://api.stripe.com/v1/");
})
.AddResiliencePolicy("stripe-gateway-policy");
```

---

## DelegatingHandler Architecture

`ResilienceDelegatingHandler` intercepts every outgoing `HttpRequestMessage`:
1. Wraps the inner `SendAsync` call with `IResilienceExecutor.ExecuteAsync(policyName, ...)`.
2. Automatically propagates the cancellation token from the resilience context.
3. Records telemetry spans and metrics for each outgoing HTTP attempt.
