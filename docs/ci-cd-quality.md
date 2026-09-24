# CI/CD, Quality Gates & Build Engineering

## 1. Build Process & Central Package Management (CPM)

The repository utilizes MSBuild Central Package Management (CPM) with unified multi-project properties configured via root files:

- **`Directory.Build.props`**: Centralizes versioning (`VersionPrefix=2.0.0`), authors, package metadata, target frameworks (`net8.0;net9.0;net10.0`), compiler warning levels (`WarningLevel=5`, `TreatWarningsAsErrors=true`), SourceLink (`SymbolPackageFormat=snupkg`), Strong Name Signing (`SignAssembly=true`), and Native AOT analyzers (`EnableTrimAnalyzer=true`, `IsAotCompatible=true`).
- **`Directory.Packages.props`**: Centrally manages all NuGet package versions across production, test, and benchmark projects (`ManagePackageVersionsCentrally=true`).

---

## 2. CI/CD Pipeline Architecture

```mermaid
flowchart TD
    subgraph FastCI ["Fast CI Pipeline (ci.yml)"]
        Trigger[Push & PR: main, develop] --> Compliance[Repo Compliance: verify-compliance.ps1]
        Compliance --> BuildTest[Build, Test & Coverage: dotnet-build-test.yml]
        BuildTest --> AotSmoke[Native AOT Smoke Test: aot-smoke-test.yml]
        BuildTest --> Codecov[Codecov Coverage Upload]
    end

    subgraph QualityGates ["Quality Gates (Audits, PRs & Scheduled)"]
        PRGate[Pull Request: main, develop] --> BenchGate[Benchmark Regression Gate: benchmark-regression-gate.yml]
        PRGate --> MutateGate[Mutation Testing Gate: mutation-testing.yml]
        ScheduledGate[Weekly Schedule / Manual Dispatch] --> WeeklyMutate[Weekly Mutation Testing: mutation-testing.yml]
        ScheduledGate --> WeeklyBench[Weekly Benchmarks Baseline: weekly-benchmarks.yml]
        MutateGate --> CommitStatus[Record Commit Status: quality-gate/mutation-testing]
        WeeklyMutate --> CommitStatus
    end

    subgraph Release ["Release Pipeline"]
        PushRelease[Push to main / Release Tag] --> ReleasePlease[Release Please: release-please.yml]
        ReleasePlease --> GitHubRelease[GitHub Release Published]
        GitHubRelease --> ValidateGate[Validate Mutation Gate: scripts/validate-mutation-gate.js]
        ValidateGate -->|Score >= 95%| PublishNuGet[Publish to NuGet: publish.yml]
        ValidateGate -.->|Score < 95%| BlockRelease[❌ Release Blocked]
    end
```

---

## 3. GitHub Actions Workflows Catalog

The repository includes 10 dedicated GitHub Actions workflow definitions:

| Workflow File | Name | Trigger | Key Jobs & Actions | Secrets Required |
|---|---|---|---|---|
| [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) | Continuous Integration | `push`, `pull_request` on `main`, `develop` | Orchestrates build/test matrix with SonarCloud, and Native AOT smoke test. | `SNK_KEY`, `CODECOV_TOKEN`, `SONAR_TOKEN` |
| [`.github/workflows/dotnet-build-test.yml`](../.github/workflows/dotnet-build-test.yml) | Reusable — .NET Build & Test | `workflow_call` | Sets up .NET 10.0.x & Java 17; restores SNK key; performs SonarCloud static analysis; builds Release; runs tests with XPlat coverage; uploads to Codecov. | `SNK_KEY`, `CODECOV_TOKEN`, `SONAR_TOKEN` |
| [`.github/workflows/aot-smoke-test.yml`](../.github/workflows/aot-smoke-test.yml) | NativeAOT Smoke Test | `workflow_call`, `push` (`main`, `develop`), `pull_request` (`main`, `develop`), `workflow_dispatch` | Publishes self-contained Linux-x64 AOT binary with `TreatWarningsAsErrors=true` and executes native smoke test assertions. | `SNK_KEY` |
| [`.github/workflows/benchmark-regression-gate.yml`](../.github/workflows/benchmark-regression-gate.yml) | Benchmark Regression Gate | `pull_request` affecting `src/**`, `benchmarks/**`, `workflow_dispatch` | Runs BenchmarkDotNet on PR head, compares vs baseline JSON via `verify-benchmark-gate.ps1`, and fails if latency regression exceeds threshold (default 5%). | `SNK_KEY` |
| [`.github/workflows/benchmarks.yml`](../.github/workflows/benchmarks.yml) | Benchmarks | `workflow_call`, `workflow_dispatch` | Runs BenchmarkDotNet with configurable filter, publishes step summaries and markdown artifacts. | `SNK_KEY` |
| [`.github/workflows/mutation-testing.yml`](../.github/workflows/mutation-testing.yml) | Mutation Testing (Stryker) | Weekly cron (`0 4 * * 1` - Monday 04:00 UTC), `workflow_call`, `workflow_dispatch` (`Basic`, `Standard`, `Advanced`) | Executes matrix Stryker mutation testing across all 7 packages as a Quality Gate; posts commit status `quality-gate/mutation-testing`. | None |
| [`.github/workflows/publish.yml`](../.github/workflows/publish.yml) | Publish NuGet | `push` (tags `v*.*.*`), `workflow_dispatch` (input: `version`) | Validates target commit mutation score (≥95%) before packing Release packages, generating Sigstore provenance attestation, and pushing to NuGet via OIDC. | `SNK_KEY`, `CODECOV_TOKEN` |
| [`.github/workflows/release-please.yml`](../.github/workflows/release-please.yml) | Release Please | `push` on `main` | Automates semantic versioning, changelog generation, GitHub releases, and triggers `publish.yml` via `workflow_dispatch`. | Standard `GITHUB_TOKEN` |
| [`.github/workflows/repo-compliance.yml`](../.github/workflows/repo-compliance.yml) | Repository Compliance & Quality Gate | `push` on `main`, `pull_request` on `main`, `workflow_dispatch` | Executes `scripts/verify-compliance.ps1`, builds with strict diagnostics, runs unit tests, and validates package creation. | None |
| [`.github/workflows/weekly-benchmarks.yml`](../.github/workflows/weekly-benchmarks.yml) | Weekly Benchmarks (Deep Review) | Weekly cron (`0 2 * * 0` - Sunday 02:00 UTC), `workflow_dispatch` | Runs multi-TFM cross-runtime benchmarks (.NET 8, 9, 10) and automatically commits updated baseline results to `benchmarks/results/`. | `SNK_KEY` |

---

## 4. Quality Gates & Enforcement

### 1. Zero Warnings Policy
All production and test code is compiled with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<WarningLevel>5</WarningLevel>`. Any compiler warning is treated as a fatal build failure.

### 2. Code Coverage Gate
Automated unit and integration tests run with Coverlet data collection (`--collect:"XPlat Code Coverage"`). Coverage reports in Cobertura format are uploaded to Codecov with strict branch, line, and method thresholds (target: 100%).

### 3. Mutation Testing Quality Gate (Stryker.NET)
Stryker is configured across 7 dedicated configuration files matching each package:
- `stryker-abstractions-config.json` — `EricksonLopez.Resilience.Abstractions`
- `stryker-core-config.json` — `EricksonLopez.Resilience` (Core)
- `stryker-polly-config.json` — `EricksonLopez.Resilience.Polly`
- `stryker-dependencyinjection-config.json` — `EricksonLopez.Resilience.DependencyInjection`
- `stryker-mediator-config.json` — `EricksonLopez.Resilience.Mediator`
- `stryker-opentelemetry-config.json` — `EricksonLopez.Resilience.OpenTelemetry`
- `stryker-aspnetcore-config.json` — `EricksonLopez.Resilience.AspNetCore`

#### Architectural Principles & Gate Operation:
1. **Quality Gate vs Fast Push**: Fast feedback on every `push` is reserved for standard CI (compliance, build, unit test coverage, and Native AOT smoke tests). Mutation testing is an explicit Quality Gate executed on Pull Requests, scheduled weekly runs, manual dispatches, or release verification — **never triggered on every push**.
2. **Quality Gate Status & Release Validation**: When executed, Stryker matrix results are saved as artifacts, rendered in Step Summaries, and published as GitHub Commit Statuses (`quality-gate/mutation-testing` and `quality-gate/mutation-testing/<package>`).
3. **Execution Profiles (`workflow_dispatch`)**:
   - `Basic`: Core packages (`core`, `abstractions`)
   - `Standard`: Core + primary adapters (`core`, `abstractions`, `polly`, `mediator`)
   - `Advanced`: Full suite across all 7 packages.
4. **Single Source of Truth Thresholds**:
   - `break = 95`: Hard failure threshold. Exit code `1` if `< 95%`.
   - `warn = 95`: Warning threshold (`95% - 97.99%` is `🟠 WARNING`).
   - `low = 98`: Low threshold (`98% - 99.99%` is `🟡 LOW`).
   - `high = 100`: `✅ HIGH`.
5. **Release Gate Validation (`publish.yml`)**:
   - Before publishing to NuGet, the release pipeline queries the commit status of the commit SHA being released using [`scripts/verify-mutation-gate.js`](../scripts/verify-mutation-gate.js).
   - If `mutation score >= 95%` -> Release is permitted.
   - If `mutation score < 95%` or unanalyzed -> Release triggers conditional Stryker run or is blocked.
   - The release does **not** re-run Stryker unnecessarily when the gate is already satisfied.

### 4. Architecture Governance Auditor
The PowerShell script [`scripts/verify-compliance.ps1`](../scripts/verify-compliance.ps1) verifies 7 critical invariants:
1. **Kebab-Case Naming**: All files in `docs/` must use lowercase kebab-case (`*.md`).
2. **Zero `[Obsolete]` APIs**: Zero obsolete attribute decorations in production code (`src/`).
3. **Canonical MIT Copyright**: Every C# file must start with `// Copyright © Erickson Lopez. MIT License.`.
4. **One Type Per File**: Exactly one top-level type declared per file in `src/`.
5. **GitHub Identity Consistency**: Project URLs must target `ericksonlopezf/dotnet-resilience`.
6. **Support & Security Email Normalization**: Official contact email must be `ericksonlopezf@gmail.com`.
7. **Prohibited Suppressions**: Zero illegal `<NoWarn>` suppressions in `.csproj` files.

### 5. Architectural Unit Testing (`NetArchTest.Rules`)
Enforced in `EricksonLopez.Resilience.ArchitectureTests`:
- `Abstractions` (L0) has zero dependencies on Polly, ASP.NET Core, or infrastructure.
- `Resilience` (L2 Core) has zero dependencies on Polly.
- Only `EricksonLopez.Resilience.Polly` (L4) references `Polly.Core`.
- All Mediator behaviors implement `IPipelineBehavior<,>` without runtime reflection.

---

## 5. Security & Supply Chain Integrity

### 1. Strong Name Signing
All production binaries are strongly named using an RSA key (`EricksonLopez.snk`). In CI environments, the key is restored from the `SNK_KEY` GitHub Secret into the repository root before compilation.

### 2. NuGet OIDC Trusted Publishing & Sigstore Provenance
- **NuGet OIDC Trusted Publishing**: Packages are pushed via `publish.yml` using GitHub Actions OpenID Connect (`NuGet/login@v1` with `id-token: write` permission), removing static API key secrets.
- **Sigstore Build Provenance Attestation**: Every `.nupkg` package is cryptographically attested via `actions/attest-build-provenance@v2.2.3` (`attestations: write`), guaranteeing verifiable origin.
- **Deterministic Builds & SourceLink**: Built with `PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, and symbol packages (`.snupkg`).
- **NuGet Vulnerability Audit**: Direct dependency scanning enabled via `<NuGetAuditMode>direct</NuGetAuditMode>` and `<NuGetAuditLevel>high</NuGetAuditLevel>`.


---

## 6. Developer Quality Commands

```bash
# 1. Clean build across all TFMs (.NET 8, 9, 10)
dotnet build EricksonLopez.Resilience.slnx --configuration Release

# 2. Execute test suite with coverage
dotnet test EricksonLopez.Resilience.slnx --configuration Release

# 3. Publish and run Native AOT Smoke Test
dotnet publish tests/EricksonLopez.Resilience.AotSmokeTest/EricksonLopez.Resilience.AotSmokeTest.csproj -c Release -r linux-x64 --self-contained -o ./aot-output
./aot-output/EricksonLopez.Resilience.AotSmokeTest

# 4. Verify architectural compliance
pwsh -File scripts/verify-compliance.ps1

# 5. Run performance benchmarks
dotnet run -c Release --project benchmarks/EricksonLopez.Resilience.Benchmarks/EricksonLopez.Resilience.Benchmarks.csproj
```

---

## 7. Branch Strategy

The following branch model is derived from the CI trigger configuration in `.github/workflows/ci.yml`:

| Branch | Purpose | CI Trigger |
|---|---|---|
| `main` | Production-ready, release-tagged code | CI (push), repo-compliance, release-please |
| `develop` | Active development integration | CI (push + PR) |
| `feature/*` | Feature development branches | PR to `develop` or `main` |
| `fix/*` | Bug-fix branches | PR to `develop` or `main` |

Release creation is automated via **release-please** (`release-please.yml`) on pushes to `main`. A GitHub Release event triggers `publish.yml` which packages and pushes all 7 NuGet packages.
