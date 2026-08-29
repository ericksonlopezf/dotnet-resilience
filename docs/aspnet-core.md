# ASP.NET Core Endpoint Resilience

## Overview

`EricksonLopez.Resilience.AspNetCore` enables declaring resilience policies directly on ASP.NET Core Minimal API endpoints and controllers via endpoint metadata.

## Usage in Minimal APIs

```csharp
using System.Threading;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/weather", async (IWeatherService weatherService, CancellationToken ct) =>
{
    return await weatherService.GetForecastAsync(ct);
})
.RequireResilience("weather-api-policy");
```

---

## Endpoint Metadata Inspection

The `RequireResilience` extension attaches `ResilienceEndpointMetadata` to the endpoint's metadata collection:

```csharp
namespace EricksonLopez.Resilience.AspNetCore.Metadata;

public sealed class ResilienceEndpointMetadata
{
    public string PolicyName { get; }
}
```

Middleware filters or endpoint invokers can inspect this metadata to enforce rate limiting, circuit breaking, or request-level timeout budgets.
