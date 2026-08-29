# Roadmap

> **Note**: This roadmap documents only items that are **formally deferred** from `v1.0.0` and recorded in Architecture Decision Records (ADRs) in [`docs/decisions/`](docs/decisions/). Completed items have been moved to [`CHANGELOG.md`](CHANGELOG.md). Items listed here are deferred — not committed delivery dates.

---

## v1.0.0 — Released

All features shipped in `v1.0.0` are documented in [`CHANGELOG.md`](CHANGELOG.md).

---

## Deferred — Post-v1.0.0 (Formally Deferred in ADRs)

The following items were explicitly analyzed and deferred to future major/minor releases during `v1.0.0` design and execution. Each deferral is justified in the referenced ADR.

| Item | Deferred In | Rationale |
|---|---|---|
| **Global Distributed Retry Budgets** | ADR-016 | Requires distributed coordination state (e.g., Redis). Incompatible with zero-dependency Abstractions contract. Deferred to `v2.x`. |
| **Standalone gRPC Integration Package** | ADR-015 | Solvable via existing `ResilienceDelegatingHandler` over standard HTTP/2 clients. No blocking gap. Deferred to `v2.x`. |
| **Separate Polly Engine Registration Assembly** | ADR-013 | Splitting the Polly adapter from `DependencyInjection` would constitute a breaking change to the existing package graph. Deferred to `v2.x`. |

---

## Explicit Non-Goals (Rejected, Not Deferred)

The following capabilities were **permanently rejected** (not scheduled) to preserve architectural integrity:

| Item | Rejected In | Rationale |
|---|---|---|
| Caching as a Resilience Strategy | ADR-011 | Violates SRP. Delegated to dedicated caching providers via `FallbackAction`. |
| Hystrix-style Dashboard UI | ADR-012 | Visualization is the responsibility of OpenTelemetry collectors (Prometheus, Grafana, Jaeger). |
| Custom Strategy Extensibility in Core | ADR-013 | Prevents API explosion and AOT trimming hazards. |
| Circuit Breaker per URL Authority | ADR-014 | HTTP routing concern belonging to HTTP client infrastructure layer. |
| Chaos Engineering in Production Runtime | ADR-017 | Fault injection belongs to dedicated test tooling (`Polly.Simmy`), not production executors. |

---

## References

- [Architecture Decision Records](docs/decisions/README.md)
- [Changelog](CHANGELOG.md)
- [Architecture Guide](docs/architecture.md)
