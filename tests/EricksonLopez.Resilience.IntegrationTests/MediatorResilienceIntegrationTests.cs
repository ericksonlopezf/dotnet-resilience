// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Mediator;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Mediator.Behaviors;
using EricksonLopez.Resilience.Mediator.Contracts;
using EricksonLopez.Resilience.Mediator.Extensions;
using EricksonLopez.Result;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Resilience.IntegrationTests;

public sealed class MediatorResilienceIntegrationTests
{
    public sealed record GetCustomerQuery(Guid CustomerId) : IQuery<Result<string>>, IResilientRequest
    {
        public string ResiliencePolicy => "customer-read-policy";
    }

    public sealed class GetCustomerQueryHandler : IQueryHandler<GetCustomerQuery, Result<string>>
    {
        private int _attempts;

        public int Attempts => _attempts;

        public ValueTask<Result<string>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
        {
            _attempts++;
            if (_attempts < 3)
            {
                throw new IOException("Transient network glitch");
            }

            return ValueTask.FromResult(Result<string>.Success($"Customer-{request.CustomerId}"));
        }
    }

    [Fact]
    public async Task ResilientRequest_ExecutedThroughMediatorBehavior_RetriesAndReturnsSuccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEricksonLopezResilience(opt =>
        {
            opt.AddPolicy("customer-read-policy", builder =>
            {
                builder.AddRetry(r =>
                {
                    r.MaxRetryAttempts = 3;
                    r.Delay = TimeSpan.FromMilliseconds(10);
                    r.ShouldHandleException = ex => ex is IOException;
                });
            });
        });

        // Register policy manually into registry
        var policyRegistry = new Resilience.Registry.ResiliencePolicyRegistry();
        var pipelineBuilder = new Builder.ResiliencePipelineBuilder("customer-read-policy");
        pipelineBuilder.AddRetry(r =>
        {
            r.MaxRetryAttempts = 3;
            r.Delay = TimeSpan.FromMilliseconds(10);
            r.ShouldHandleException = ex => ex is IOException;
        });
        var pipeline = pipelineBuilder.Build();

        services.AddSingleton(pipeline);
        services.AddSingleton<IResiliencePipelineRegistry>(sp =>
        {
            var reg = new Resilience.Registry.ResiliencePipelineRegistry();
            reg.Register("customer-read-policy", pipeline);
            return reg;
        });

        var handler = new GetCustomerQueryHandler();
        var behavior = new ResiliencePipelineBehavior<GetCustomerQuery, Result<string>>(
            new Polly.Adapters.PollyResilienceExecutor(new Resilience.Registry.ResiliencePipelineRegistry().Register("customer-read-policy", pipeline)));

        var query = new GetCustomerQuery(Guid.NewGuid());

        // Act
        var continuation = new TestNext<Result<string>>(() => handler.Handle(query, CancellationToken.None));
        var response = await behavior.Handle(query, continuation, CancellationToken.None);

        // Assert
        response.IsSuccess.Should().BeTrue();
        response.Value.Should().StartWith("Customer-");
        handler.Attempts.Should().Be(3);
    }

    private readonly struct TestNext<T> : INext<T>
    {
        private readonly Func<ValueTask<T>> _callback;

        public TestNext(Func<ValueTask<T>> callback)
        {
            _callback = callback;
        }

        public ValueTask<T> InvokeAsync() => _callback();
    }
}
