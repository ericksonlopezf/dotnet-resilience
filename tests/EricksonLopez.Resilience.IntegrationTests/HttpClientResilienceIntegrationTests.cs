// Copyright © Erickson Lopez. MIT License.
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.AspNetCore.Http;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using Xunit;

namespace EricksonLopez.Resilience.IntegrationTests;

public sealed class HttpClientResilienceIntegrationTests
{
    private sealed class TransientHttpMessageHandler : HttpMessageHandler
    {
        private int _attempts;

        public int Attempts => _attempts;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _attempts++;
            if (_attempts < 2)
            {
                // Throw transient 503 HttpRequestException
                throw new HttpRequestException("Service Temporarily Unavailable", null, HttpStatusCode.ServiceUnavailable);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"success\"}")
            });
        }
    }

    [Fact]
    public async Task ResilienceDelegatingHandler_WithTransientHttpFailure_RetriesAndReturnsSuccessfulResponse()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("http-client-policy");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleResult = res => res is HttpResponseMessage { StatusCode: HttpStatusCode.ServiceUnavailable };
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new Resilience.Registry.ResiliencePipelineRegistry();
        registry.Register("http-client-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);
        var innerHandler = new TransientHttpMessageHandler();
        var delegatingHandler = new ResilienceDelegatingHandler(executor, "http-client-policy")
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(delegatingHandler);

        // Act
        var response = await client.GetAsync("https://api.ericksonlopez.dev/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.Attempts.Should().Be(2);
    }
}
