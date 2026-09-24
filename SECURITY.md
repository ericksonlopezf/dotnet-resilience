# Security Policy

## Supported Versions

We provide security updates and patches for the following versions of `EricksonLopez.Resilience`:

| Version | Supported | Target Frameworks | Notes |
|---|:---:|---|---|
| `2.0.x` | :white_check_mark: | `.NET 8.0` (`net8.0`), `.NET 9.0` (`net9.0`), `.NET 10.0` (`net10.0`) | Current active release line. |
| `1.0.x` | :warning: | `.NET 8.0` (`net8.0`), `.NET 9.0` (`net9.0`), `.NET 10.0` (`net10.0`) | Maintenance mode (critical security fixes only). |
| `< 1.0.0` | :x: | Pre-release | Not supported. |

---

## Reporting a Vulnerability

If you discover a security vulnerability within `EricksonLopez.Resilience`, please follow these steps:

1. **Do NOT disclose the issue publicly** (do not open public GitHub issues or discussions).
2. Send a detailed report directly via email to: [ericksonlopezf@gmail.com](mailto:ericksonlopezf@gmail.com).
3. Include the following details in your report:
   - Package name and version.
   - Type of vulnerability (e.g. denial of service, resource exhaustion, sensitive data exposure).
   - Step-by-step instructions or minimal reproducible proof-of-concept.
   - Any proposed mitigations or workarounds.

### Response Timeline
- **Initial Acknowledgement**: Within 48 hours.
- **Vulnerability Assessment & Triage**: Within 5 business days.
- **Patch Release & Advisory Publication**: Coordinated with the reporter upon resolution.

---

## Supply Chain Security

1. **Strong Name Signing**: All published assemblies are strongly named using an RSA key (`EricksonLopez.snk`). In CI environments, the key is restored from the `SNK_KEY` GitHub Secret (Base64-encoded) before compilation, guaranteeing binary authenticity and tamper evidence.
2. **Deterministic Builds & SourceLink**: Builds use `PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, and `IncludeSymbols=true` (`.snupkg` format) to embed source maps and emit symbol packages mapped to exact Git commit SHAs.
3. **NuGet OIDC Trusted Publishing**: Packages are published via `publish.yml` using OpenID Connect (OIDC) authentication (`NuGet/login@v1` with `id-token: write` permission), eliminating static, long-lived API keys from CI secrets.
4. **Sigstore Provenance Attestation**: Published packages are attested with cryptographically verifiable build provenance generated via `actions/attest-build-provenance@v2.2.3` (`attestations: write`), guaranteeing that packages originated from this official repository workflow run.
5. **NuGet Vulnerability Audit**: Direct dependency vulnerability scanning is enabled across all projects (`<NuGetAuditMode>direct</NuGetAuditMode>`, `<NuGetAuditLevel>high</NuGetAuditLevel>`), flagging known CVEs at compilation time.

---

## Security Architecture & Boundaries

1. **Thread Safety**: All pipeline registries (`ResiliencePipelineRegistry`, `ResiliencePolicyRegistry`) and execution engines are thread-safe and safe for high-concurrency multi-threaded environments.
2. **Context & State Isolation**: `ResilienceContext` instances carry transient metadata and cancellation tokens. They should not be shared across concurrent un-related operations.
3. **No Secret Leakage**: `ResilienceActivitySource` and `ResilienceMeter` emit semantic span tags and metrics containing operation names, policy names, and tenant identifiers. Sensitive payload data is never captured or serialized into telemetry tags.
4. **Denial of Service Prevention**:
   - `RateLimiterStrategy` provides deterministic request shedding under saturation.
   - `TimeoutStrategy` guarantees execution timeouts across distributed calls.
   - `CircuitBreakerStrategy` protects upstream and downstream services from cascading collapse.
