# ADR-015: Deferral of EricksonLopez.Resilience.Grpc Integration

## Status
Deferred — Revisit when documented user demand exists

## Date
2026-09-04

## Context
gRPC is a widely adopted inter-service communication protocol in microservice architectures. A `ClientInterceptor` implementation that wraps gRPC calls with ecosystem resilience pipelines was considered as an integration layer analogous to `ResilienceDelegatingHandler` for HttpClient.

## Decision
**A dedicated `EricksonLopez.Resilience.Grpc` package is explicitly deferred.** No gRPC interceptor will be included in the current package surface.

Rationale:
1. **Low Proven Demand**: There are no documented user requests or GitHub issues indicating that gRPC resilience integration is a blocking concern for the target segment (DDD/Clean Architecture applications primarily using HTTP and Mediator patterns).
2. **Grpc.Core Already Supports Polly**: Polly policies can be applied to gRPC channels directly via `GrpcChannelOptions.HttpHandler`. Consumers can use `ResilienceDelegatingHandler` (already available in `EricksonLopez.Resilience.AspNetCore`) as the underlying handler, achieving the same result without a dedicated package.
3. **Maintenance Overhead**: gRPC interceptors require separate dependency on `Grpc.Core.Api` and generate a new test matrix dimension (unary, server streaming, client streaming, bidirectional streaming calls). This is disproportionate to the current adoption stage.

## Revisit Criteria
- At least 3 documented user requests for gRPC resilience integration.
- Confirmed inability to achieve gRPC resilience via `ResilienceDelegatingHandler` composition.
- Sufficient adoption of the core package to justify expanding the integration surface.
