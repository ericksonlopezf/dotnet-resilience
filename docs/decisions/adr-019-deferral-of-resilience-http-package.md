# ADR-019: Deferral of EricksonLopez.Resilience.Http Package (L2)

## Status
Deferred — Planned for v1.3.0 pending adoption signals

## Date
2026-09-04

## Context
`ResilienceDelegatingHandler` and `HttpClientResilienceExtensions` (including `AddStandardResilienceHandler()`) currently live in `EricksonLopez.Resilience.AspNetCore`. However, these components only require `Microsoft.Extensions.Http`, not the full ASP.NET Core stack. This means that Worker Services, Console applications, and Azure Functions cannot use HTTP resilience integration without taking a dependency on ASP.NET Core.

A proposed `EricksonLopez.Resilience.Http` package would extract these components so that any `IHttpClientFactory`-based host — without ASP.NET Core — could use the HTTP resilience handler.

## Decision
**The `EricksonLopez.Resilience.Http` package is explicitly deferred.** HTTP resilience remains in `EricksonLopez.Resilience.AspNetCore` for v1.x.

Rationale:
1. **Low Current Impact**: The target segment (DDD applications with Clean Architecture) is predominantly ASP.NET Core based. Worker Services or Console-only consumers are an uncommon use case at the current adoption stage.
2. **Non-Breaking Deferral**: Maintaining HTTP resilience in the `AspNetCore` package is a conservative choice that avoids proliferating packages unnecessarily. ASP.NET Core is already a standard dependency in most .NET 10 services.
3. **Package Proliferation Risk**: Each additional package adds versioning, documentation, NuGet publishing, and CI pipeline complexity. At the current adoption stage, adding a 9th package for a niche use case is premature.
4. **Re-export Strategy Viable**: When `EricksonLopez.Resilience.Http` is eventually created, `AspNetCore` can re-export via a project reference without breaking existing consumers.

## Revisit Criteria
- Documented user requests from Worker Service or Console-app consumers needing HTTP resilience without ASP.NET Core.
- Overall package adoption reaches a level where the maintenance overhead of a 9th package is justified.
- v1.3.0 release window planning.
