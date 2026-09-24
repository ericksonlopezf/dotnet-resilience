## Description

Please include a summary of the change and which issue is fixed (if any).
Include relevant motivation and context.

## Affected Packages
Please check all packages that are affected by this PR:
- [ ] `EricksonLopez.Resilience` (Core)
- [ ] `EricksonLopez.Resilience.Abstractions`
- [ ] `EricksonLopez.Resilience.AspNetCore`
- [ ] `EricksonLopez.Resilience.DependencyInjection`
- [ ] `EricksonLopez.Resilience.Mediator`
- [ ] `EricksonLopez.Resilience.OpenTelemetry`
- [ ] `EricksonLopez.Resilience.Polly`

## Checklist

Before submitting this PR, please verify the following:
- [ ] I have performed a self-review of my own code.
- [ ] I have updated the `CHANGELOG.md` (if applicable).
- [ ] I have added/updated unit tests or integration tests.
- [ ] Local build passes (`dotnet build EricksonLopez.Resilience.slnx -c Release`).
- [ ] Local tests pass (`dotnet test EricksonLopez.Resilience.slnx`).
- [ ] I verified compliance using `./scripts/verify-compliance.ps1`.
- [ ] Stryker mutation testing maintains the **95%** mutation score threshold.
- [ ] Benchmarks confirmed no regressions.
