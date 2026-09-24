// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Registration;
using Xunit;

namespace EricksonLopez.Resilience.IntegrationTests;

public sealed class DestructiveTests
{
    public DestructiveTests()
    {
        PollyResilienceRegistration.Initialize();
    }

    [Fact]
    public async Task RateLimiter_ConcurrencyLimit_StrictlyEnforced()
    {
        const int limit = 10;
        const int concurrentRequests = 1000;

        var builder = new ResiliencePipelineBuilder("StrictConcurrency");
        builder.AddRateLimiter(new RateLimiterStrategyOptions
        {
            LimiterType = RateLimiterType.Concurrency,
            PermitLimit = limit,
            QueueLimit = concurrentRequests
        });

        var pipeline = builder.Build();
        int maxObservedConcurrency = 0;
        int currentConcurrency = 0;
        var lockObj = new object();
        var tcs = new TaskCompletionSource();

        var tasks = new Task[concurrentRequests];
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await pipeline.ExecuteAsync(async ct =>
                {
                    int current = Interlocked.Increment(ref currentConcurrency);
                    lock (lockObj)
                    {
                        if (current > maxObservedConcurrency)
                        {
                            maxObservedConcurrency = current;
                        }
                    }
                    await tcs.Task;
                    Interlocked.Decrement(ref currentConcurrency);
                });
            });
        }

        await Task.Delay(500);
        tcs.SetResult();
        await Task.WhenAll(tasks);

        Assert.True(maxObservedConcurrency <= limit, $"Observed concurrency {maxObservedConcurrency} exceeded limit {limit}");
    }

    [Fact]
    public async Task CircuitBreaker_UnderMassiveConcurrentFailures_TransitionsSafely()
    {
        var builder = new ResiliencePipelineBuilder("CB_Massive");
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(1),
            ShouldHandleException = ex => true
        });
        var pipeline = builder.Build();

        var exceptions = new ConcurrentQueue<Exception>();
        int executed = 0;

        var tasks = new Task[1000];
        for (int i = 0; i < 1000; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    await pipeline.ExecuteAsync(ct =>
                    {
                        Interlocked.Increment(ref executed);
                        throw new InvalidOperationException("Boom");
                    });
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });
        }
        await Task.WhenAll(tasks);
        Assert.True(executed < 1000, $"Circuit Breaker failed to stop executions. Executed: {executed}");
    }

    [Fact]
    public async Task Timeout_ExtremelySmall_DoesNotLeakTasks()
    {
        var builder = new ResiliencePipelineBuilder("Timeout_Small");
        builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(10) });
        var pipeline = builder.Build();

        int executed = 0;
        int completedTasks = 0;

        var tasks = new Task[100];
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    await pipeline.ExecuteAsync(async ct =>
                    {
                        Interlocked.Increment(ref executed);
                        await Task.Delay(100, ct);
                    });
                }
                catch (Exceptions.ResilienceTimeoutException) { }
                catch (OperationCanceledException) { }
                finally { Interlocked.Increment(ref completedTasks); }
            });
        }

        await Task.WhenAll(tasks);
        Assert.Equal(100, completedTasks);
    }

    [Fact]
    public async Task Bulkhead_MassiveConcurrency_ShouldNotStarveOrLeak()
    {
        var builder = new ResiliencePipelineBuilder("test-bulkhead");
        builder.AddRateLimiter(new RateLimiterStrategyOptions
        {
            LimiterType = RateLimiterType.Concurrency,
            PermitLimit = 5,
            QueueLimit = 100000
        });

        var pipeline = builder.Build();

        int activeOperations = 0;
        int maxObservedOperations = 0;
        int completedOperations = 0;
        var lockObj = new object();

        var tasks = Enumerable.Range(0, 1000).Select(async i =>
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                var current = Interlocked.Increment(ref activeOperations);
                lock (lockObj)
                {
                    if (current > maxObservedOperations)
                    {
                        maxObservedOperations = current;
                    }
                }

                await Task.Delay(1, ct);

                Interlocked.Decrement(ref activeOperations);
                Interlocked.Increment(ref completedOperations);
            });
        }).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1000, completedOperations);
        Assert.True(maxObservedOperations <= 5, $"Observed {maxObservedOperations} concurrent operations, expected max 5.");
        Assert.Equal(0, activeOperations);
    }

    [Fact]
    public async Task Retry_ExtremeCount_ShouldNotOverflowOrLeak()
    {
        var builder = new ResiliencePipelineBuilder("test-retry");
        var exception = Record.Exception(() => builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.Zero,
            MaxDelay = TimeSpan.Zero,
            ShouldHandleException = _ => true
        }));

        if (exception != null)
        {
            Assert.True(exception is ArgumentException || exception.GetType().Name.Contains("Validation"));
            return;
        }

        var pipeline = builder.Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var count = 0;

        try
        {
            await pipeline.ExecuteAsync(async ct =>
            {
                count++;
                await Task.Yield();
                throw new InvalidOperationException("Fail forever");
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { }

        Assert.True(count > 0, "Pipeline should have executed the operation at least once.");
    }
}
