# Level 09: Extensions — ASP.NET Core, HttpClient & OpenTelemetry

## 1. Resilient HttpClient Integration
Attach resilience policies to typed or named `HttpClient` instances via `AddResiliencePolicy` or `AddStandardResilienceHandler`:

```csharp
using System;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEricksonLopezResilience();

// Option A: Named policy delegating handler
services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri("https://api.weather.com/");
})
.AddResiliencePolicy("weather-api-policy");

// Option B: Standard HTTP Resilience Handler preset (Timeout + Retry + Circuit Breaker + Rate Limiter)
services.AddHttpClient("PaymentGatewayApi", client =>
{
    client.BaseAddress = new Uri("https://api.payments.com/");
})
.AddStandardResilienceHandler("http-standard-payments", builder =>
{
    builder.AddTimeout(TimeSpan.FromSeconds(15));
});
```

---

## 2. Minimal API Endpoint Metadata
Declare resilience requirements directly on ASP.NET Core endpoints:

```csharp
app.MapPost("/api/orders/checkout", async (CheckoutRequest request, IMediator mediator) =>
{
    var result = await mediator.SendAsync(new CheckoutOrderCommand(request));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
})
.RequireResilience("enterprise-checkout-policy");
```

---

## 3. OpenTelemetry Metrics & Distributed Tracing
Telemetry callbacks and metrics emit standard W3C semantic attributes:

```csharp
using EricksonLopez.Resilience.OpenTelemetry;
using EricksonLopez.Resilience.OpenTelemetry.Extensions;

// Attaching telemetry hooks to strategy options:
var retryOpt = new RetryStrategyOptions { MaxRetryAttempts = 3 }.WithTelemetry();
var cbOpt = new CircuitBreakerStrategyOptions { FailureRatio = 0.5 }.WithTelemetry();
var timeoutOpt = new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(5) }.WithTelemetry();

// Distributed Activity Tracing:
using var activity = ResilienceActivitySource.StartExecutionActivity(
    "payment-policy", 
    "ProcessCharge", 
    tenantId: "TENANT-01", 
    correlationId: "CORR-9988");
```
