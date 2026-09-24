// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Resilience.AspNetCore.HealthChecks;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EricksonLopez.Resilience.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for registering resilience circuit breaker health checks in ASP.NET Core applications.
/// </summary>
public static class CircuitBreakerHealthCheckExtensions
{
    /// <summary>
    /// Adds a resilience circuit breaker health check to the health check builder.
    /// </summary>
    /// <param name="builder">The health check builder.</param>
    /// <param name="name">The health check name.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="stateAccessor">A delegate that retrieves the current circuit breaker state.</param>
    /// <param name="failureStatus">The failure status to report when the circuit is open.</param>
    /// <param name="tags">Optional tags for the health check.</param>
    /// <returns>The health check builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="stateAccessor"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> or <paramref name="policyName"/> is <see langword="null"/> or whitespace</exception>
    public static IHealthChecksBuilder AddCircuitBreakerCheck(
        this IHealthChecksBuilder builder,
        string name,
        string policyName,
        Func<CircuitBreakerState> stateAccessor,
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(stateAccessor);

        return tags == null
            ? builder.AddCheck(name, new ResilienceCircuitBreakerHealthCheck(policyName, stateAccessor), failureStatus)
            : builder.AddCheck(name, new ResilienceCircuitBreakerHealthCheck(policyName, stateAccessor), failureStatus, tags);
    }
}
