// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Result;
using Xunit;

namespace EricksonLopez.Resilience.IntegrationTests;

public sealed class IdempotencyResilienceIntegrationTests
{
    private sealed class MockPaymentGateway
    {
        private readonly ConcurrentDictionary<string, string> _processedPayments = new();
        private int _networkDropsRemaining = 1;
        private int _totalExecutions;

        public int TotalExecutions => _totalExecutions;

        public ValueTask<Result<string>> ProcessPaymentAsync(string idempotencyKey, decimal amount, CancellationToken ct)
        {
            Interlocked.Increment(ref _totalExecutions);

            // If previously committed, return existing idempotent transaction reference
            if (_processedPayments.TryGetValue(idempotencyKey, out var existingTxId))
            {
                return ValueTask.FromResult(Result<string>.Success(existingTxId));
            }

            // Generate new transaction ID and record
            var txId = $"TX-{Guid.NewGuid():N}";
            _processedPayments[idempotencyKey] = txId;

            // Simulate transient network failure during response transmission
            if (_networkDropsRemaining > 0)
            {
                _networkDropsRemaining--;
                throw new IOException("Socket reset after remote server committed payment.");
            }

            return ValueTask.FromResult(Result<string>.Success(txId));
        }
    }

    [Fact]
    public async Task ResilientRetry_WithPreservedIdempotencyKey_PreventsDuplicateSideEffects()
    {
        // Arrange: Pipeline with retry strategy
        var builder = new ResiliencePipelineBuilder("payment-policy");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(10);
            opt.ShouldHandleException = ex => ex is IOException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var gateway = new MockPaymentGateway();
        var idempotencyKey = "PAY-REQ-998877";

        // Act: Resilient execution preserving idempotency key across retry attempts
        var result = await pipeline.ExecuteAsync<Result<string>>(async ct =>
        {
            return await gateway.ProcessPaymentAsync(idempotencyKey, 150.00m, ct);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("TX-");

        // Gateway executed 2 times (attempt 1 failed post-commit, attempt 2 retrieved idempotent response)
        gateway.TotalExecutions.Should().Be(2);
    }
}
