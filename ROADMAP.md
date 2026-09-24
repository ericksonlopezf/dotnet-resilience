# Product Roadmap

> **Note**: This roadmap documents only items that are **formally deferred** from `v1.0.0` and recorded in Architecture Decision Records (ADRs) in [`docs/decisions/`](docs/decisions/). Completed items have been moved to [`CHANGELOG.md`](CHANGELOG.md). Items listed here are deferred — not committed delivery dates.

---

## v2.0.0 — Released

All features shipped in `v2.0.0` are documented in [`CHANGELOG.md`](CHANGELOG.md).

---

## v1.0.0 — Released

All features shipped in `v1.0.0` are documented in [`CHANGELOG.md`](CHANGELOG.md).

---

## Deferred — Post-v1.0.0 (Formally Deferred in ADRs)

The following items were explicitly analyzed and deferred to future major/minor releases during `v1.0.0` design and execution. Each deferral is justified in the referenced ADR.

| Item | Deferred In | Rationale |
|---|---|---|
| **Global Distributed Retry Budgets** | [ADR-016](docs/decisions/adr-016-deferral-of-global-retry-budget.md) | Requires distributed coordination state (e.g., Redis). Incompatible with zero-dependency Abstractions contract. Deferred to `v2.x`. |
| **Standalone gRPC Integration Package** | [ADR-015](docs/decisions/adr-015-deferral-of-grpc-integration.md) | Solvable via existing `ResilienceDelegatingHandler` over standard HTTP/2 clients. No blocking gap. Deferred to `v2.x`. |
| **Separate Polly Engine Registration Assembly** | [ADR-018](docs/decisions/adr-018-deferral-of-di-polly-engine-separation.md) | Splitting the Polly adapter from `DependencyInjection` would constitute a breaking change to the existing package graph. Batched for `v2.0.0`. |
| **Standalone EricksonLopez.Resilience.Http Package** | [ADR-019](docs/decisions/adr-019-deferral-of-resilience-http-package.md) | Extracting HTTP client handlers from `AspNetCore` for Worker Services/Console hosts is deferred to avoid premature package proliferation. Planned for `v1.3.0+`. |

---

## Explicit Non-Goals (Rejected, Not Deferred)

The following capabilities were **permanently rejected** (not scheduled) to preserve architectural integrity:

| Item | Rejected In | Rationale |
|---|---|---|
| **Caching as a Resilience Strategy** | [ADR-011](docs/decisions/adr-011-rejection-of-caching-as-resilience-strategy.md) | Violates SRP. Delegated to dedicated caching providers via `FallbackAction`. |
| **Hystrix-style Dashboard UI** | [ADR-012](docs/decisions/adr-012-rejection-of-dashboard-ui.md) | Visualization is the responsibility of OpenTelemetry collectors (Prometheus, Grafana, Jaeger). |
| **Custom Strategy Extensibility in Core** | [ADR-013](docs/decisions/adr-013-rejection-of-custom-strategy-extensibility.md) | Prevents API explosion and AOT trimming hazards. |
| **Circuit Breaker per URL Authority** | [ADR-014](docs/decisions/adr-014-rejection-of-circuit-breaker-pool-per-url-authority.md) | HTTP routing concern belonging to HTTP client infrastructure layer. |
| **Chaos Engineering in Production Runtime** | [ADR-017](docs/decisions/adr-017-rejection-of-chaos-engineering.md) | Fault injection belongs to dedicated test tooling (`Polly.Simmy`), not production executors. |

---

## References

- [Architecture Decision Records](docs/decisions/README.md)
- [Changelog](CHANGELOG.md)
- [Architecture Guide](docs/architecture.md)
