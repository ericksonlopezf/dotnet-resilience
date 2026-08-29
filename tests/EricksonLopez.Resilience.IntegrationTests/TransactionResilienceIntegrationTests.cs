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

public sealed class TransactionResilienceIntegrationTests
{
    private sealed class MockDatabaseTransaction : IAsyncDisposable
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public bool IsCommitted { get; private set; }
        public bool IsRolledBack { get; private set; }
        public bool IsDisposed { get; private set; }

        public ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            if (IsRolledBack || IsDisposed)
            {
                throw new InvalidOperationException("Cannot commit an aborted or disposed transaction.");
            }

            IsCommitted = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            IsRolledBack = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MockUnitOfWork
    {
        public List<Guid> CreatedTransactionIds { get; } = new();

        public async ValueTask<MockDatabaseTransaction> BeginTransactionAsync()
        {
            var tx = new MockDatabaseTransaction();
            CreatedTransactionIds.Add(tx.TransactionId);
            return await ValueTask.FromResult(tx);
        }
    }

    [Fact]
    public async Task ResilientTransactionExecution_CreatesNewTransactionScopePerRetryAttempt()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("db-tx-policy");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(10);
            opt.ShouldHandleException = ex => ex is IOException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var unitOfWork = new MockUnitOfWork();
        var attempt = 0;

        // Act: Resilience wraps the transaction boundary (Resilience -> UnitOfWork/Transaction)
        var result = await pipeline.ExecuteAsync<Result<string>>(async ct =>
        {
            attempt++;
            await using var tx = await unitOfWork.BeginTransactionAsync();

            if (attempt < 3)
            {
                // Simulate a transient network failure during operation execution
                await tx.RollbackAsync(ct);
                throw new IOException("Connection terminated by database cluster failover.");
            }

            await tx.CommitAsync(ct);
            return Result<string>.Success("OrderProcessed");
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("OrderProcessed");

        // Verify that 3 distinct transactions were instantiated (one per attempt, never reusing an aborted transaction)
        unitOfWork.CreatedTransactionIds.Should().HaveCount(3);
        unitOfWork.CreatedTransactionIds.Should().OnlyHaveUniqueItems();
    }
}
