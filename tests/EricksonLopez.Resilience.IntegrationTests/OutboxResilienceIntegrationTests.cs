// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Result;
using Xunit;

namespace EricksonLopez.Resilience.IntegrationTests;

public sealed class OutboxResilienceIntegrationTests
{
    private sealed class OutboxMessage
    {
        public Guid MessageId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }

    private sealed class MockResilientOrderService
    {
        public List<string> PersistedOrders { get; } = new();
        public List<OutboxMessage> PersistedOutboxMessages { get; } = new();
        private int _transientFailures = 1;

        public ValueTask<Result<string>> CreateOrderWithOutboxAsync(string orderId, CancellationToken ct)
        {
            if (_transientFailures > 0)
            {
                _transientFailures--;
                throw new IOException("Database lock timeout during transactional commit.");
            }

            // Both aggregate and outbox message committed atomically
            PersistedOrders.Add(orderId);
            PersistedOutboxMessages.Add(new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                EventType = "OrderCreatedDomainEvent",
                Payload = $"{{\"orderId\":\"{orderId}\"}}"
            });

            return ValueTask.FromResult(Result<string>.Success(orderId));
        }
    }

    [Fact]
    public async Task ResilientOrderCreation_PersistsAggregateAndOutboxAtomicallyAcrossRetries()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("outbox-order-policy");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is IOException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var service = new MockResilientOrderService();
        var orderId = "ORD-776655";

        // Act
        var result = await pipeline.ExecuteAsync<Result<string>>(async ct =>
        {
            return await service.CreateOrderWithOutboxAsync(orderId, ct);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(orderId);

        // Exactly 1 order and 1 outbox message persisted atomically
        service.PersistedOrders.Should().ContainSingle().Which.Should().Be(orderId);
        service.PersistedOutboxMessages.Should().ContainSingle().Which.EventType.Should().Be("OrderCreatedDomainEvent");
    }
}
