// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Result;
using Xunit;

namespace EricksonLopez.Resilience.IntegrationTests;

public sealed class ConcurrencyResilienceIntegrationTests
{
    private sealed class ConcurrencyConflictException : Exception
    {
        public ConcurrencyConflictException(string message) : base(message) { }
    }

    private sealed class BankAccount
    {
        public decimal Balance { get; set; } = 1000m;
        public int Version { get; set; } = 1;
    }

    private sealed class AccountRepository
    {
        private readonly BankAccount _account = new();
        private int _simulatedConcurrentWrites = 1;

        public BankAccount ReadAccount() => new() { Balance = _account.Balance, Version = _account.Version };

        public ValueTask<Result<bool>> UpdateAccountAsync(decimal newBalance, int expectedVersion)
        {
            if (_simulatedConcurrentWrites > 0)
            {
                _simulatedConcurrentWrites--;
                _account.Version++; // Another thread modified the aggregate version
                throw new ConcurrencyConflictException("Optimistic concurrency conflict detected. Aggregate was modified concurrently.");
            }

            if (_account.Version != expectedVersion)
            {
                throw new ConcurrencyConflictException("Version mismatch.");
            }

            _account.Balance = newBalance;
            _account.Version++;
            return ValueTask.FromResult(Result<bool>.Success(true));
        }
    }

    [Fact]
    public async Task OptimisticConcurrencyConflict_WithFreshStateReloadOnRetry_SucceedsDeterministically()
    {
        // Arrange
        var builder = new ResiliencePipelineBuilder("concurrency-policy");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.Delay = TimeSpan.FromMilliseconds(5);
            opt.ShouldHandleException = ex => ex is ConcurrencyConflictException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var repository = new AccountRepository();
        var attempts = 0;

        // Act: Resilient loop re-reads latest aggregate state on each attempt before calculating mutation
        var result = await pipeline.ExecuteAsync<Result<bool>>(async ct =>
        {
            attempts++;

            // 1. Re-read latest aggregate state
            var currentAccount = repository.ReadAccount();

            // 2. Calculate business mutation
            var updatedBalance = currentAccount.Balance - 100m;

            // 3. Commit with version verification
            return await repository.UpdateAccountAsync(updatedBalance, currentAccount.Version);
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        attempts.Should().Be(2); // Attempt 1 conflicted, Attempt 2 re-read version 2 and succeeded
    }
}
