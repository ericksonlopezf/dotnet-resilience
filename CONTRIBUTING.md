# Contributing to EricksonLopez.Resilience

Thank you for your interest in contributing to `EricksonLopez.Resilience`! We welcome community contributions, bug reports, and enhancements.

---

## Prerequisites

- **.NET 10.0 SDK** (with support for multi-targeting `net8.0`, `net9.0`, and `net10.0`).
- C# 13 / 14 support.
- Any modern IDE (Visual Studio 2022 / VS Code with C# Dev Kit / JetBrains Rider).
- PowerShell 7+ (`pwsh`) for running repository governance and compliance scripts.

---

## Repository Structure

```
dotnet-resilience/
├── src/                                  # Production library packages (multi-targeting net8.0;net9.0;net10.0)
│   ├── EricksonLopez.Resilience.Abstractions/
│   ├── EricksonLopez.Resilience/
│   ├── EricksonLopez.Resilience.Polly/
│   ├── EricksonLopez.Resilience.DependencyInjection/
│   ├── EricksonLopez.Resilience.Mediator/
│   ├── EricksonLopez.Resilience.OpenTelemetry/
│   └── EricksonLopez.Resilience.AspNetCore/
├── samples/                              # Reference applications
│   └── Showcase/                         # Progressive levels (0-10) and Cookbook
├── tests/                                # Test suites
│   ├── EricksonLopez.Resilience.Abstractions.Tests/
│   ├── EricksonLopez.Resilience.Tests/
│   ├── EricksonLopez.Resilience.Polly.Tests/
│   ├── EricksonLopez.Resilience.DependencyInjection.Tests/
│   ├── EricksonLopez.Resilience.Mediator.Tests/
│   ├── EricksonLopez.Resilience.OpenTelemetry.Tests/
│   ├── EricksonLopez.Resilience.AspNetCore.Tests/
│   ├── EricksonLopez.Resilience.IntegrationTests/
│   ├── EricksonLopez.Resilience.ArchitectureTests/
│   └── EricksonLopez.Resilience.AotSmokeTest/
├── benchmarks/                           # Performance benchmarks
│   └── EricksonLopez.Resilience.Benchmarks/
├── docs/                                 # Architectural and technical documentation
│   ├── decisions/                        # Architecture Decision Records (ADRs)
│   └── showcase/                         # Step-by-step showcase guides
└── scripts/                              # Quality, mutation, and compliance verification scripts
```

---

## Build & Test Commands

### 1. Build Solution
```bash
dotnet build EricksonLopez.Resilience.slnx --configuration Release
```

### 2. Run Test Suite
```bash
dotnet test EricksonLopez.Resilience.slnx --configuration Release
```

### 3. Run Native AOT Smoke Test
```bash
dotnet publish tests/EricksonLopez.Resilience.AotSmokeTest/EricksonLopez.Resilience.AotSmokeTest.csproj -c Release -r linux-x64 --self-contained -o ./aot-output
./aot-output/EricksonLopez.Resilience.AotSmokeTest
```

### 4. Run Executable Showcase
```bash
dotnet run --project samples/Showcase/EricksonLopez.Resilience.Showcase.csproj -- all
```

### 5. Run Benchmarks
```bash
dotnet run -c Release --project benchmarks/EricksonLopez.Resilience.Benchmarks/EricksonLopez.Resilience.Benchmarks.csproj
```

### 6. Run Mutation Testing (per package)
```bash
# Install Stryker.NET CLI (once)
dotnet tool install -g dotnet-stryker

# Run per-package mutation testing
dotnet-stryker --config-file stryker-abstractions-config.json
dotnet-stryker --config-file stryker-core-config.json
dotnet-stryker --config-file stryker-polly-config.json
dotnet-stryker --config-file stryker-dependencyinjection-config.json
dotnet-stryker --config-file stryker-mediator-config.json
dotnet-stryker --config-file stryker-opentelemetry-config.json
dotnet-stryker --config-file stryker-aspnetcore-config.json
```

### 7. Verify Architecture & Repository Compliance
```powershell
pwsh -File scripts/verify-compliance.ps1
```

---

## Development Standards & Guidelines

1. **Zero Warnings Policy**: The repository compiles with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. All builds must produce 0 warnings and 0 errors.
2. **Clean Architecture Isolation**:
   - `Abstractions` (L0) must have 0 external dependencies (only .NET BCL `System.Threading.RateLimiting`).
   - `Core` (L2) must never reference Polly or third-party engines.
   - `Polly` (L4) is strictly an infrastructure adapter.
3. **Native AOT & Trimming**: No runtime reflection on the critical execution path. Maintain `<IsAotCompatible>true</IsAotCompatible>` and `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>`.
4. **Documentation**:
   - XML doc comments are required on all public types and members.
   - All documentation in `docs/` must use English and kebab-case filenames.
5. **Code Style**:
   - Follow standard `.editorconfig` rules.
   - Use file-scoped namespaces, nullable reference types, and expression-bodied members where appropriate.

---

## Branching & Commit Conventions

- **Branch Naming**:
  - `feature/<short-description>`
  - `fix/<short-description>`
  - `refactor/<short-description>`
  - `docs/<short-description>`
- **Commit Messages**: Follow [Conventional Commits](https://www.conventionalcommits.org/):
  - `feat(retry): add exponential jitter support`
  - `fix(circuit-breaker): handle half-open transition delay`
  - `docs(architecture): update layer segregation diagram`

---

## Pull Request Checklist

Before submitting a Pull Request, please ensure:

- [ ] Solution compiles cleanly (`dotnet build EricksonLopez.Resilience.slnx -c Release`) with 0 warnings.
- [ ] All automated tests pass (`dotnet test EricksonLopez.Resilience.slnx -c Release`).
- [ ] Architecture tests pass (`EricksonLopez.Resilience.ArchitectureTests`).
- [ ] Native AOT smoke test compiles and passes (`dotnet publish tests/EricksonLopez.Resilience.AotSmokeTest/EricksonLopez.Resilience.AotSmokeTest.csproj -c Release -r linux-x64 --self-contained`).
- [ ] Compliance script passes with 0 violations (`pwsh -File scripts/verify-compliance.ps1`).
- [ ] Mutation score maintained ≥ 95% break threshold (100% target) for affected packages.
- [ ] Benchmarks confirmed no regressions (latency regression $\le 5\%$ vs baseline).
- [ ] New public APIs include XML documentation comments.
- [ ] Showcase reference examples are updated if public API changed.
- [ ] Commit history is clean and follows conventional commit messages.


---

## Security Policy

To report a vulnerability, please follow the [Security Policy](SECURITY.md). Do not open public GitHub issues for security reports.

---

## Code of Conduct

All contributors are expected to adhere to the [Code of Conduct](CODE_OF_CONDUCT.md).
