// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EricksonLopez.Resilience.AspNetCore.HealthChecks;

/// <summary>
/// Provides an ASP.NET Core <see cref="IHealthCheck"/> implementation that monitors resilience circuit breaker states.
/// </summary>
public sealed class ResilienceCircuitBreakerHealthCheck : IHealthCheck
{
    private readonly Func<CircuitBreakerState> _stateAccessor;
    private readonly string _policyName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceCircuitBreakerHealthCheck"/> class.
    /// </summary>
    /// <param name="policyName">The logical policy name.</param>
    /// <param name="stateAccessor">The delegate returning the current circuit breaker state.</param>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="stateAccessor"/> is <see langword="null"/></exception>
    public ResilienceCircuitBreakerHealthCheck(string policyName, Func<CircuitBreakerState> stateAccessor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        _policyName = policyName;
        _stateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
    }

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var state = _stateAccessor();
        var data = new Dictionary<string, object>
        {
            ["resilience.policy"] = _policyName,
            ["resilience.circuit.state"] = state.ToString()
        };

        return state switch
        {
            CircuitBreakerState.Closed => Task.FromResult(HealthCheckResult.Healthy($"Circuit breaker '{_policyName}' is Closed (operational).", data)),
            CircuitBreakerState.HalfOpen => Task.FromResult(HealthCheckResult.Degraded($"Circuit breaker '{_policyName}' is Half-Open (probing).", data: data)),
            CircuitBreakerState.Open => Task.FromResult(HealthCheckResult.Unhealthy($"Circuit breaker '{_policyName}' is Open (fast-rejecting).", data: data)),
            CircuitBreakerState.Isolated => Task.FromResult(HealthCheckResult.Unhealthy($"Circuit breaker '{_policyName}' is Isolated (manually disabled).", data: data)),
            _ => Task.FromResult(HealthCheckResult.Unhealthy($"Circuit breaker '{_policyName}' is in an unknown state.", data: data))
        };
    }
}
