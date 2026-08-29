// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.AspNetCore.Extensions;
using EricksonLopez.Resilience.AspNetCore.Http;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.AspNetCore.Tests;

public sealed class HttpClientResilienceExtensionsTests
{
    static HttpClientResilienceExtensionsTests()
    {
        PollyResilienceRegistration.Initialize();
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    #region AddResiliencePolicy

    [Fact]
    public void AddResiliencePolicy_NullBuilder_ThrowsArgumentNullException()
    {
        IHttpClientBuilder? builder = null;
        Action act = () => builder!.AddResiliencePolicy("test-policy");
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddResiliencePolicy_NullOrWhitespacePolicyName_ThrowsArgumentException(string? policyName)
    {
        var services = new ServiceCollection();
        var builder = services.AddHttpClient("TestClient");
        Action act = () => builder.AddResiliencePolicy(policyName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddResiliencePolicy_RegistersDelegatingHandlerInHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();
        services.AddResiliencePolicy("test-http-policy", builder =>
        {
            builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromSeconds(10));
        });

        // Act
        var returned = services.AddHttpClient("TestClient")
            .AddResiliencePolicy("test-http-policy");

        // Assert
        returned.Should().NotBeNull();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("TestClient");
        client.Should().NotBeNull();
    }

    #endregion

    #region AddStandardResilienceHandler

    [Fact]
    public void AddStandardResilienceHandler_NullBuilder_ThrowsArgumentNullException()
    {
        IHttpClientBuilder? builder = null;
        Action act = () => builder!.AddStandardResilienceHandler();
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null, "http-standard-ExternalService")]
    [InlineData("", "http-standard-ExternalService")]
    [InlineData("   ", "http-standard-ExternalService")]
    [InlineData("custom-http-policy", "custom-http-policy")]
    public void AddStandardResilienceHandler_WithVariousPolicyNames_RegistersCorrectPolicyNameInRegistry(
        string? policyName,
        string expectedPolicyName)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        var configuredCalled = false;
        IResiliencePipelineBuilder? capturedBuilder = null;

        // Act
        var returned = services.AddHttpClient("ExternalService")
            .AddStandardResilienceHandler(policyName, b =>
            {
                configuredCalled = true;
                capturedBuilder = b;
            });

        // Assert
        returned.Should().NotBeNull();

        // Build provider and resolve registry to trigger pipeline compilation
        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IResiliencePipelineRegistry>();
        registry.TryGetPipeline(expectedPolicyName, out var pipeline).Should().BeTrue();
        pipeline.Should().NotBeNull();

        configuredCalled.Should().BeTrue();
        capturedBuilder.Should().NotBeNull();
        capturedBuilder!.Name.Should().Be(expectedPolicyName);

        // Verify standard strategies configured
        capturedBuilder.Strategies.Should().HaveCount(4);

        // 1. Timeout
        var timeoutStrategy = capturedBuilder.Strategies.OfType<TimeoutStrategyOptions>().FirstOrDefault();
        timeoutStrategy.Should().NotBeNull();
        timeoutStrategy!.Timeout.Should().Be(TimeSpan.FromSeconds(30));

        // 2. Retry
        var retryStrategy = capturedBuilder.Strategies.OfType<RetryStrategyOptions>().FirstOrDefault();
        retryStrategy.Should().NotBeNull();
        retryStrategy!.MaxRetryAttempts.Should().Be(3);
        retryStrategy.Delay.Should().Be(TimeSpan.FromMilliseconds(500));
        retryStrategy.BackoffType.Should().Be(BackoffType.ExponentialWithJitter);
        retryStrategy.MaxDelay.Should().Be(TimeSpan.FromSeconds(5));

        // 3. Circuit Breaker
        var cbStrategy = capturedBuilder.Strategies.OfType<CircuitBreakerStrategyOptions>().FirstOrDefault();
        cbStrategy.Should().NotBeNull();
        cbStrategy!.FailureRatio.Should().Be(0.5);
        cbStrategy.MinimumThroughput.Should().Be(10);
        cbStrategy.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        cbStrategy.BreakDuration.Should().Be(TimeSpan.FromSeconds(15));

        // 4. Rate Limiter
        var rateStrategy = capturedBuilder.Strategies.OfType<RateLimiterStrategyOptions>().FirstOrDefault();
        rateStrategy.Should().NotBeNull();
        rateStrategy!.PermitLimit.Should().Be(1000);
        rateStrategy.QueueLimit.Should().Be(100);
        rateStrategy.Window.Should().Be(TimeSpan.FromMinutes(1));

        // Verify HttpClient can be resolved
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("ExternalService");
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddStandardResilienceHandler_WithoutConfigureDelegate_RegistersSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        // Act
        var returned = services.AddHttpClient("DefaultClient")
            .AddStandardResilienceHandler();

        // Assert
        returned.Should().NotBeNull();

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IResiliencePipelineRegistry>();
        registry.TryGetPipeline("http-standard-DefaultClient", out var pipeline).Should().BeTrue();
        pipeline.Should().NotBeNull();
    }

    #endregion
}
