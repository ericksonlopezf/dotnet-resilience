# Packages Reference

## 1. Published Packages

All 7 packages in the `EricksonLopez.Resilience` ecosystem target `net8.0;net9.0;net10.0` and are published to [NuGet.org](https://www.nuget.org/).

| Package | NuGet Layer | NuGet |
|---|---|:---:|
| [`EricksonLopez.Resilience.Abstractions`](../src/EricksonLopez.Resilience.Abstractions) | L0 Foundation | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.Abstractions.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience.Abstractions) |
| [`EricksonLopez.Resilience`](../src/EricksonLopez.Resilience) | L2 Core | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience) |
| [`EricksonLopez.Resilience.Polly`](../src/EricksonLopez.Resilience.Polly) | L4 Infrastructure | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.Polly.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience.Polly) |
| [`EricksonLopez.Resilience.DependencyInjection`](../src/EricksonLopez.Resilience.DependencyInjection) | L3 Integration | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.DependencyInjection.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience.DependencyInjection) |
| [`EricksonLopez.Resilience.Mediator`](../src/EricksonLopez.Resilience.Mediator) | L3 Integration | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.Mediator.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience.Mediator) |
| [`EricksonLopez.Resilience.OpenTelemetry`](../src/EricksonLopez.Resilience.OpenTelemetry) | L3 Observability | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.OpenTelemetry.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience.OpenTelemetry) |
| [`EricksonLopez.Resilience.AspNetCore`](../src/EricksonLopez.Resilience.AspNetCore) | L3 Presentation | [![NuGet](https://img.shields.io/nuget/v/EricksonLopez.Resilience.AspNetCore.svg)](https://www.nuget.org/packages/EricksonLopez.Resilience.AspNetCore) |

---

## 2. Framework Compatibility Matrix

All packages are multi-targeted and fully compatible across all three LTS/Current .NET generations.

| Package | .NET 8.0 | .NET 9.0 | .NET 10.0 | Native AOT | Trimming-Safe |
|---|:---:|:---:|:---:|:---:|:---:|
| `EricksonLopez.Resilience.Abstractions` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EricksonLopez.Resilience` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EricksonLopez.Resilience.Polly` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EricksonLopez.Resilience.DependencyInjection` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EricksonLopez.Resilience.Mediator` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EricksonLopez.Resilience.OpenTelemetry` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `EricksonLopez.Resilience.AspNetCore` | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 3. Central Package Management — Pinned Dependency Versions

All production and test dependency versions are centrally managed in [`Directory.Packages.props`](../Directory.Packages.props) (`ManagePackageVersionsCentrally=true`).

### Production Dependencies

| Package | Pinned Version | Used By |
|---|---|---|
| `Polly.Core` | `8.5.2` | `EricksonLopez.Resilience.Polly` (L4 only) |
| `Polly.RateLimiting` | `8.5.2` | `EricksonLopez.Resilience.Polly` (L4 only) |
| `System.Threading.RateLimiting` | `10.0.11` | `EricksonLopez.Resilience.Abstractions` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.11` | DI, Mediator |
| `Microsoft.Extensions.DependencyInjection` | `10.0.11` | DI |
| `Microsoft.Extensions.Configuration` | `10.0.11` | DI |
| `Microsoft.Extensions.Configuration.Abstractions` | `10.0.11` | DI |
| `Microsoft.Extensions.Options` | `10.0.11` | DI |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.11` | DI |
| `Microsoft.Extensions.Http` | `10.0.11` | AspNetCore |
| `Microsoft.AspNetCore.Http.Abstractions` | `2.3.0` | AspNetCore |
| `OpenTelemetry.Api` | `1.11.2` | OpenTelemetry |

### Test & Benchmark Dependencies

| Package | Pinned Version | Used By |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | `17.14.1` | All test projects |
| `xunit` | `2.9.3` | All test projects |
| `xunit.runner.visualstudio` | `3.0.2` | All test projects |
| `AwesomeAssertions` | `9.6.0` | All test projects |
| `NSubstitute` | `5.3.0` | Unit test projects |
| `NetArchTest.Rules` | `1.3.2` | Architecture tests |
| `coverlet.collector` | `6.0.4` | All test projects (coverage) |
| `BenchmarkDotNet` | `0.15.8` | Benchmarks project |

---

## 4. Installation Guide

### Minimal Installation (Framework with DI)

```bash
dotnet add package EricksonLopez.Resilience.DependencyInjection
```

This transitively includes `EricksonLopez.Resilience.Abstractions`, `EricksonLopez.Resilience`, and `EricksonLopez.Resilience.Polly`.

### Optional Extensions

```bash
# OpenTelemetry metrics and distributed tracing
dotnet add package EricksonLopez.Resilience.OpenTelemetry

# ASP.NET Core endpoint metadata and resilient HttpClient
dotnet add package EricksonLopez.Resilience.AspNetCore

# EricksonLopez.Mediator pipeline behavior integration
dotnet add package EricksonLopez.Resilience.Mediator
```

---

## 5. Benchmarks

Performance benchmarks are located in [`benchmarks/EricksonLopez.Resilience.Benchmarks/`](../benchmarks/EricksonLopez.Resilience.Benchmarks/) using [BenchmarkDotNet](https://benchmarkdotnet.org/) v0.15.8.

To run the full benchmark suite:

```bash
dotnet run -c Release --project benchmarks/EricksonLopez.Resilience.Benchmarks/EricksonLopez.Resilience.Benchmarks.csproj
```

Baseline results are stored in `benchmarks/results/` and automatically updated by the weekly benchmarks workflow (`weekly-benchmarks.yml`). The benchmark regression gate (`benchmark-regression-gate.yml`) compares PR changes against the stored baseline and fails if regression exceeds 10%.

---

## 6. Samples

The repository includes an official executable reference application at [`samples/Showcase/`](../samples/Showcase/):

```bash
# Run all progressive learning levels (0-10) and cookbook recipes
dotnet run --project samples/Showcase/EricksonLopez.Resilience.Showcase.csproj -- all

# Run a specific level (e.g., level 3)
dotnet run --project samples/Showcase/EricksonLopez.Resilience.Showcase.csproj -- 3

# Run only the cookbook production recipes
dotnet run --project samples/Showcase/EricksonLopez.Resilience.Showcase.csproj -- cookbook
```

See [`docs/showcase/`](showcase/) for the step-by-step documentation for each level.
