// Copyright © Erickson Lopez. MIT License.
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.AspNetCore.Http;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Resilience.Polly.Registration;
using EricksonLopez.Resilience.Registry;
using Xunit;

namespace EricksonLopez.Resilience.AspNetCore.Tests;

public sealed class ResilienceDelegatingHandlerTests
{
    static ResilienceDelegatingHandlerTests()
    {
        PollyResilienceRegistration.Initialize();
    }

    private sealed class MockHttpHandler : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;
        public CancellationToken LastToken { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public int FailuresBeforeSuccess { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _calls++;
            LastRequest = request;
            LastToken = cancellationToken;

            if (_calls <= FailuresBeforeSuccess)
            {
                throw new HttpRequestException("Transient error", null, HttpStatusCode.ServiceUnavailable);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class InspectingResilienceExecutor : IResilienceExecutor
    {
        public string? LastPolicyName { get; private set; }
        public ResilienceContext? LastContext { get; private set; }

        public ValueTask<TResult> ExecuteAsync<TResult>(
            string policyName,
            Func<ResilienceContext, ValueTask<TResult>> action,
            ResilienceContext? context = null,
            CancellationToken cancellationToken = default)
        {
            LastPolicyName = policyName;
            LastContext = context;
            return action(context ?? ResilienceContext.Create(policyName, cancellationToken: cancellationToken));
        }

        public ValueTask ExecuteAsync(
            string policyName,
            Func<ResilienceContext, ValueTask> action,
            ResilienceContext? context = null,
            CancellationToken cancellationToken = default)
        {
            LastPolicyName = policyName;
            LastContext = context;
            return action(context ?? ResilienceContext.Create(policyName, cancellationToken: cancellationToken));
        }

        public ValueTask<TResult> ExecuteAsync<TResult>(
            string policyName,
            Func<CancellationToken, ValueTask<TResult>> action,
            CancellationToken cancellationToken = default)
        {
            LastPolicyName = policyName;
            return action(cancellationToken);
        }

        public ValueTask ExecuteAsync(
            string policyName,
            Func<CancellationToken, ValueTask> action,
            CancellationToken cancellationToken = default)
        {
            LastPolicyName = policyName;
            return action(cancellationToken);
        }
    }

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullExecutor_ThrowsArgumentNullException()
    {
        IResilienceExecutor? executor = null;
        Action act = () => _ = new ResilienceDelegatingHandler(executor!, "test-policy");
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrWhitespacePolicyName_ThrowsArgumentException(string? policyName)
    {
        var registry = new ResiliencePipelineRegistry();
        var executor = new PollyResilienceExecutor(registry);
        Action act = () => _ = new ResilienceDelegatingHandler(executor, policyName!);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region SendAsync Execution Tests

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        var mockExecutor = new InspectingResilienceExecutor();
        var handler = new ResilienceDelegatingHandler(mockExecutor, "my-policy")
        {
            InnerHandler = new MockHttpHandler()
        };

        var sendAsyncMethod = typeof(ResilienceDelegatingHandler).GetMethod(
            "SendAsync",
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(HttpRequestMessage), typeof(CancellationToken)]);

        sendAsyncMethod.Should().NotBeNull();

        Func<Task> act = async () =>
        {
            try
            {
                var task = (Task<HttpResponseMessage>)sendAsyncMethod!.Invoke(handler, [null!, CancellationToken.None])!;
                await task;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        };

        var ex = await act.Should().ThrowAsync<ArgumentNullException>();
        ex.Which.ParamName.Should().Be("request");
    }

    [Fact]
    public async Task SendAsync_WithUri_PassesContextWithOperationNameAndPolicy()
    {
        // Arrange
        var mockExecutor = new InspectingResilienceExecutor();
        var innerHandler = new MockHttpHandler();
        var handler = new ResilienceDelegatingHandler(mockExecutor, "orders-policy")
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.orders.example.com/v1/checkout");
        using var cts = new CancellationTokenSource();

        // Act
        var response = await invoker.SendAsync(request, cts.Token);

        // Assert
        mockExecutor.LastPolicyName.Should().Be("orders-policy");
        mockExecutor.LastContext.Should().NotBeNull();
        mockExecutor.LastContext!.PolicyName.Should().Be("orders-policy");
        mockExecutor.LastContext.OperationName.Should().Be("HTTP POST api.orders.example.com");
        mockExecutor.LastContext.CancellationToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task SendAsync_WithNullRequestUri_PassesContextWithOperationNameWithoutHost()
    {
        // Arrange
        var mockExecutor = new InspectingResilienceExecutor();
        var innerHandler = new MockHttpHandler();
        var handler = new ResilienceDelegatingHandler(mockExecutor, "relative-policy")
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, (Uri?)null);

        // Act
        var response = await invoker.SendAsync(request, CancellationToken.None);

        // Assert
        mockExecutor.LastPolicyName.Should().Be("relative-policy");
        mockExecutor.LastContext.Should().NotBeNull();
        mockExecutor.LastContext!.OperationName.Should().Be("HTTP GET ");
    }

    [Fact]
    public async Task SendAsync_RetriesTransientHttpRequestException_AndReturns200OK()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("http-test-policy");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is HttpRequestException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("http-test-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);
        var innerHandler = new MockHttpHandler { FailuresBeforeSuccess = 1 };
        var handler = new ResilienceDelegatingHandler(executor, "http-test-policy")
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(handler);

        // Act
        var response = await client.GetAsync("https://example.com/api");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        innerHandler.Calls.Should().Be(2);
    }

    #endregion
}
