// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.AspNetCore.HealthChecks;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace EricksonLopez.Resilience.AspNetCore.Tests;

public sealed class ResilienceCircuitBreakerHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenClosed_ReturnsHealthy()
    {
        var check = new ResilienceCircuitBreakerHealthCheck("orders-cb", () => CircuitBreakerState.Closed);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Closed");
        result.Data["resilience.policy"].Should().Be("orders-cb");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHalfOpen_ReturnsDegraded()
    {
        var check = new ResilienceCircuitBreakerHealthCheck("orders-cb", () => CircuitBreakerState.HalfOpen);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Half-Open");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOpen_ReturnsUnhealthy()
    {
        var check = new ResilienceCircuitBreakerHealthCheck("orders-cb", () => CircuitBreakerState.Open);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Open");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenIsolated_ReturnsUnhealthy()
    {
        var check = new ResilienceCircuitBreakerHealthCheck("orders-cb", () => CircuitBreakerState.Isolated);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Isolated");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidPolicyName_ThrowsArgumentException(string? invalidName)
    {
        var act = () => new ResilienceCircuitBreakerHealthCheck(invalidName!, () => CircuitBreakerState.Closed);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullStateAccessor_ThrowsArgumentNullException()
    {
        var act = () => new ResilienceCircuitBreakerHealthCheck("test-policy", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_VerifiesDataDictionaryKeysAndValues()
    {
        var check = new ResilienceCircuitBreakerHealthCheck("orders-cb", () => CircuitBreakerState.Closed);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Data.Should().ContainKey("resilience.policy");
        result.Data["resilience.policy"].Should().Be("orders-cb");
        result.Data.Should().ContainKey("resilience.circuit.state");
        result.Data["resilience.circuit.state"].Should().Be("Closed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnknownState_ReturnsUnhealthy()
    {
        var check = new ResilienceCircuitBreakerHealthCheck("orders-cb", () => (CircuitBreakerState)255);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Circuit breaker 'orders-cb' is in an unknown state.");
        result.Data["resilience.circuit.state"].Should().Be("255");
    }

    [Fact]
    public void AddCircuitBreakerCheck_NullBuilder_ThrowsArgumentNullException()
    {
        IHealthChecksBuilder builder = null!;
        var act = () => builder.AddCircuitBreakerCheck("name", "policy", () => CircuitBreakerState.Closed);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCircuitBreakerCheck_InvalidName_ThrowsArgumentException(string? invalidName)
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        var act = () => builder.AddCircuitBreakerCheck(invalidName!, "policy", () => CircuitBreakerState.Closed);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCircuitBreakerCheck_InvalidPolicyName_ThrowsArgumentException(string? invalidPolicy)
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        var act = () => builder.AddCircuitBreakerCheck("name", invalidPolicy!, () => CircuitBreakerState.Closed);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddCircuitBreakerCheck_NullStateAccessor_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        var builder = services.AddHealthChecks();
        var act = () => builder.AddCircuitBreakerCheck("name", "policy", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCircuitBreakerCheck_RegistersHealthCheckOnServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks()
            .AddCircuitBreakerCheck("payments_cb_check", "payments-cb", () => CircuitBreakerState.Closed);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();
        var registration = options.Value.Registrations.Should().Contain(r => r.Name == "payments_cb_check").Subject;
        registration.FailureStatus.Should().Be(HealthStatus.Unhealthy);
        registration.Tags.Should().BeEmpty();
    }

    [Fact]
    public void AddCircuitBreakerCheck_WithFailureStatusAndTags_RegistersCorrectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var tags = new[] { "ready", "resilience" };
        services.AddHealthChecks()
            .AddCircuitBreakerCheck("payments_cb_check", "payments-cb", () => CircuitBreakerState.Closed, HealthStatus.Degraded, tags);

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();
        var registration = options.Value.Registrations.Should().Contain(r => r.Name == "payments_cb_check").Subject;
        registration.FailureStatus.Should().Be(HealthStatus.Degraded);
        registration.Tags.Should().BeEquivalentTo(tags);
    }
}
