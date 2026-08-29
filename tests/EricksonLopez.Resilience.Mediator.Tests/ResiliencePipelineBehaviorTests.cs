// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Resilience.Polly.Registration;
using EricksonLopez.Resilience.Registry;
using EricksonLopez.Result;
using Xunit;

namespace EricksonLopez.Resilience.Mediator.Tests;

public sealed class ResiliencePipelineBehaviorTests
{
    static ResiliencePipelineBehaviorTests()
    {
        PollyResilienceRegistration.Initialize();
    }

    private sealed record NonResilientQuery() : IQuery<Result<string>>;

    private sealed record ResilientCommand(string PolicyName) : ICommand<Result<string>>, IResilientRequest
    {
        public string ResiliencePolicy => PolicyName;
    }

    private struct CountingMockNext : INext<Result<string>>
    {
        private readonly Func<ValueTask<Result<string>>> _func;

        public CountingMockNext(Func<ValueTask<Result<string>>> func)
        {
            _func = func;
        }

        public ValueTask<Result<string>> InvokeAsync() => _func();
    }

    [Fact]
    public void Constructor_WithNullExecutor_ThrowsArgumentNullException()
    {
        IResilienceExecutor? executor = null;
        Action act = () => _ = new ResiliencePipelineBehavior<NonResilientQuery, Result<string>>(executor!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotResilient_PassesDirectlyThrough()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var executor = new PollyResilienceExecutor(registry);
        var behavior = new ResiliencePipelineBehavior<NonResilientQuery, Result<string>>(executor);

        var query = new NonResilientQuery();
        var invoked = false;
        var next = new CountingMockNext(() =>
        {
            invoked = true;
            return ValueTask.FromResult(Result<string>.Success("DirectPass"));
        });

        // Act
        var response = await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        invoked.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be("DirectPass");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenResilientRequestHasNullOrWhitespacePolicyName_PassesDirectlyThrough(string? policyName)
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry();
        var executor = new PollyResilienceExecutor(registry);
        var behavior = new ResiliencePipelineBehavior<ResilientCommand, Result<string>>(executor);

        var command = new ResilientCommand(policyName!);
        var invoked = false;
        var next = new CountingMockNext(() =>
        {
            invoked = true;
            return ValueTask.FromResult(Result<string>.Success("WhitespaceBypass"));
        });

        // Act
        var response = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        invoked.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be("WhitespaceBypass");
    }

    [Fact]
    public async Task Handle_WhenRequestIsResilient_ExecutesThroughPipelineWithRetryProtection()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("retry-policy");
        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(10)
        });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        var registry = new ResiliencePipelineRegistry();
        registry.Register("retry-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);
        var behavior = new ResiliencePipelineBehavior<ResilientCommand, Result<string>>(executor);

        var command = new ResilientCommand("retry-policy");
        var attempts = 0;
        var next = new CountingMockNext(() =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new TimeoutException("Transient error in handler");
            }
            return ValueTask.FromResult(Result<string>.Success("Recovered"));
        });

        // Act
        var response = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        attempts.Should().Be(3);
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be("Recovered");
    }

    [Fact]
    public async Task Handle_WhenRequestIsResilient_EnforcesRateLimiterThroughPipeline()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("rate-policy");
        builder.AddRateLimiter(new RateLimiterStrategyOptions
        {
            PermitLimit = 1,
            QueueLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        var registry = new ResiliencePipelineRegistry();
        registry.Register("rate-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);
        var behavior = new ResiliencePipelineBehavior<ResilientCommand, Result<string>>(executor);

        var command = new ResilientCommand("rate-policy");
        var next = new CountingMockNext(() => ValueTask.FromResult(Result<string>.Success("Permitted")));

        // 1st request consumes permit
        var res1 = await behavior.Handle(command, next, CancellationToken.None);
        res1.IsSuccess.Should().BeTrue();
        res1.Value.Should().Be("Permitted");

        // 2nd request exceeds permit limit
        Func<Task> act = async () => await behavior.Handle(command, next, CancellationToken.None);
        await act.Should().ThrowAsync<RateLimitRejectedException>();
    }

    private sealed class CapturingResilienceExecutor : IResilienceExecutor
    {
        public ResilienceContext? LastContext { get; private set; }
        public string? LastPolicyName { get; private set; }

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

    [Fact]
    public async Task Handle_WhenRequestIsResilient_PassesContextWithOperationNameAndCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var mockExecutor = new CapturingResilienceExecutor();
        var behavior = new ResiliencePipelineBehavior<ResilientCommand, Result<string>>(mockExecutor);

        var command = new ResilientCommand("inspect-policy");
        var next = new CountingMockNext(() => ValueTask.FromResult(Result<string>.Success("OK")));

        // Act
        var response = await behavior.Handle(command, next, cts.Token);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().Be("OK");

        mockExecutor.LastPolicyName.Should().Be("inspect-policy");
        mockExecutor.LastContext.Should().NotBeNull();
        mockExecutor.LastContext!.PolicyName.Should().Be("inspect-policy");
        mockExecutor.LastContext.OperationName.Should().Be(nameof(ResilientCommand));
        mockExecutor.LastContext.CancellationToken.Should().Be(cts.Token);
    }
}
