// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Resilience.Exceptions;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Resilience.Registry;
using global::Polly;
using Microsoft.Extensions.Logging;
using PollyCircuitBreaker = global::Polly.CircuitBreaker;
using EcoBuilder = EricksonLopez.Resilience.Builder.ResiliencePipelineBuilder;
using Xunit;

namespace EricksonLopez.Resilience.Polly.Tests;

public sealed class PollyResilienceExecutorTests
{
    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new PollyResilienceExecutor(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithNullOrWhitespacePolicyName_ThrowsArgumentException(string? invalidPolicy)
    {
        var fakeRegistry = new FakeNonValidatingRegistry();
        var executor = new PollyResilienceExecutor(fakeRegistry);
        var context = ResilienceContext.Create("test");

        Func<Task> act1 = async () => await executor.ExecuteAsync<string>(invalidPolicy!, ctx => ValueTask.FromResult("ok"), context);
        await act1.Should().ThrowAsync<ArgumentException>();

        Func<Task> act2 = async () => await executor.ExecuteAsync<string>(invalidPolicy!, ct => ValueTask.FromResult("ok"));
        await act2.Should().ThrowAsync<ArgumentException>();

        Func<Task> act3 = async () => await executor.ExecuteAsync(invalidPolicy!, ctx => ValueTask.CompletedTask, context);
        await act3.Should().ThrowAsync<ArgumentException>();

        Func<Task> act4 = async () => await executor.ExecuteAsync(invalidPolicy!, ct => ValueTask.CompletedTask);
        await act4.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperationOrContext_ThrowsArgumentNullException()
    {
        var registry = new ResiliencePipelineRegistry();
        var executor = new PollyResilienceExecutor(registry);
        var context = ResilienceContext.Create("test");

        Func<Task> act1 = async () => await executor.ExecuteAsync<string>("test", (Func<ResilienceContext, ValueTask<string>>)null!, context);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act2 = async () => await executor.ExecuteAsync<string>("test", ctx => ValueTask.FromResult("ok"), null!);
        await act2.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act3 = async () => await executor.ExecuteAsync<string>("test", (Func<CancellationToken, ValueTask<string>>)null!);
        await act3.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act4 = async () => await executor.ExecuteAsync("test", (Func<ResilienceContext, ValueTask>)null!, context);
        await act4.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act5 = async () => await executor.ExecuteAsync("test", ctx => ValueTask.CompletedTask, null!);
        await act5.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act6 = async () => await executor.ExecuteAsync("test", (Func<CancellationToken, ValueTask>)null!);
        await act6.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithRegisteredPolicy_ExecutesSuccessfully()
    {
        // Arrange
        var builder = new EcoBuilder("exec-policy");
        builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromSeconds(5) });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("exec-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);

        // Act
        var result = await executor.ExecuteAsync(
            "exec-policy",
            async ctx =>
            {
                await Task.Yield();
                return "Executed";
            });

        // Assert
        result.Should().Be("Executed");
    }

    [Fact]
    public async Task ExecuteAsync_WithLogger_LogsTraceAndDebugOnSuccess()
    {
        // Arrange
        var builder = new EcoBuilder("logged-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("logged-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);
        var context = new ResilienceContext("logged-policy", operationName: "FetchCustomer", tenantId: "tenant-alpha", correlationId: "corr-100");

        // Act
        var result = await executor.ExecuteAsync("logged-policy", ctx => ValueTask.FromResult("Data"), context);

        // Assert
        result.Should().Be("Data");
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Trace &&
            e.EventId.Name == "ResilienceExecuting" &&
            e.Message.Contains("Executing resilient operation FetchCustomer with policy logged-policy (Tenant: tenant-alpha, Correlation: corr-100)"));
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Debug &&
            e.EventId.Name == "ResilienceSuccess" &&
            e.Message.Contains("Resilient operation FetchCustomer with policy logged-policy succeeded in"));
    }

    [Fact]
    public async Task ExecuteAsync_WithNullTenantAndCorrelation_LogsNone()
    {
        // Arrange
        var builder = new EcoBuilder("logged-none-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("logged-none-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);
        var context = new ResilienceContext("logged-none-policy", operationName: "FetchAnonymous", tenantId: null, correlationId: null);

        // Act
        var result = await executor.ExecuteAsync("logged-none-policy", ctx => ValueTask.FromResult("Data"), context);

        // Assert
        result.Should().Be("Data");
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Trace &&
            e.EventId.Name == "ResilienceExecuting" &&
            e.Message.Contains("Executing resilient operation FetchAnonymous with policy logged-none-policy (Tenant: None, Correlation: None)"));
    }

    [Fact]
    public async Task ExecuteAsync_WithLoggerDisabled_DoesNotLog()
    {
        // Arrange
        var builder = new EcoBuilder("no-log-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("no-log-policy", pipeline);

        var disabledLogger = new SpyResilienceLogger(minLevel: LogLevel.None);
        var executor = new PollyResilienceExecutor(registry, disabledLogger);
        var context = new ResilienceContext("no-log-policy", operationName: "FetchData");

        // Act
        var result = await executor.ExecuteAsync("no-log-policy", ctx => ValueTask.FromResult("Data"), context);

        // Assert
        result.Should().Be("Data");
        disabledLogger.LoggedEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimeoutOccurs_LogsWarningAndThrows()
    {
        // Arrange
        var builder = new EcoBuilder("timeout-fail-policy");
        builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(20) });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("timeout-fail-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        // Act & Assert Generic
        Func<Task> act = async () =>
        {
            await executor.ExecuteAsync<string>("timeout-fail-policy", async ct =>
            {
                await Task.Delay(200, ct);
                return "late";
            });
        };
        await act.Should().ThrowAsync<ResilienceTimeoutException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceTimeout" &&
            e.Message.Contains("timed out after"));

        // Act & Assert Void
        Func<Task> actVoid = async () =>
        {
            await executor.ExecuteAsync("timeout-fail-policy", async ct =>
            {
                await Task.Delay(200, ct);
            });
        };
        await actVoid.Should().ThrowAsync<ResilienceTimeoutException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitBrokenOccurs_LogsWarningAndThrows()
    {
        // Arrange
        var builder = new EcoBuilder("cb-fail-policy");
        builder.AddCircuitBreaker(opt =>
        {
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 2;
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.BreakDuration = TimeSpan.FromMilliseconds(500);
            opt.ShouldHandleException = ex => ex is InvalidOperationException;
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("cb-fail-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        for (var i = 0; i < 2; i++)
        {
            try
            {
                await executor.ExecuteAsync("cb-fail-policy", (Func<CancellationToken, ValueTask<string>>)(ct => throw new InvalidOperationException("Fail")));
            }
            catch (InvalidOperationException)
            {
            }
        }

        // Act & Assert Generic
        Func<Task> act = async () => await executor.ExecuteAsync("cb-fail-policy", (Func<CancellationToken, ValueTask<string>>)(ct => ValueTask.FromResult("ok")));
        await act.Should().ThrowAsync<CircuitBrokenException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceCircuitBroken" &&
            e.Message.Contains("blocked because circuit breaker cb-fail-policy is open"));

        // Act & Assert Void
        Func<Task> actVoid = async () => await executor.ExecuteAsync("cb-fail-policy", (Func<CancellationToken, ValueTask>)(ct => ValueTask.CompletedTask));
        await actVoid.Should().ThrowAsync<CircuitBrokenException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateLimitOccurs_LogsWarningAndThrows()
    {
        // Arrange
        var builder = new EcoBuilder("rl-fail-policy");
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = RateLimiterType.FixedWindow;
            opt.PermitLimit = 1;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMinutes(1);
        });

        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("rl-fail-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        // 1st passes
        await executor.ExecuteAsync("rl-fail-policy", ct => ValueTask.CompletedTask);

        // 2nd Generic fails
        Func<Task> act = async () => await executor.ExecuteAsync("rl-fail-policy", ct => ValueTask.FromResult("two"));
        await act.Should().ThrowAsync<RateLimitRejectedException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceRateLimited" &&
            e.Message.Contains("rejected by rate limiter for policy rl-fail-policy"));

        // 3rd Void fails
        Func<Task> actVoid = async () => await executor.ExecuteAsync("rl-fail-policy", ct => ValueTask.CompletedTask);
        await actVoid.Should().ThrowAsync<RateLimitRejectedException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenGeneralExceptionOccurs_LogsErrorAndThrows()
    {
        // Arrange
        var builder = new EcoBuilder("gen-fail-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("gen-fail-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);
        var context = new ResilienceContext("gen-fail-policy", operationName: "FetchCustomer");

        // Generic
        Func<Task> act = async () =>
        {
            await executor.ExecuteAsync<string>("gen-fail-policy", ct => throw new InvalidOperationException("General error"), context);
        };
        await act.Should().ThrowAsync<InvalidOperationException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Error &&
            e.EventId.Name == "ResilienceFailed" &&
            e.Message.Contains("Resilient operation FetchCustomer with policy gen-fail-policy failed after"));

        // Void
        Func<Task> actVoid = async () =>
        {
            await executor.ExecuteAsync("gen-fail-policy", ct => throw new InvalidOperationException("General error"), context);
        };
        await actVoid.Should().ThrowAsync<InvalidOperationException>();

        spyLogger.LoggedEntries.Count(e => e.Level == LogLevel.Error && e.EventId.Name == "ResilienceFailed").Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithActivityListener_RecordsActivityStatusOnError()
    {
        // Arrange
        var stoppedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "EricksonLopez.Resilience",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = act => stoppedActivities.Add(act)
        };
        ActivitySource.AddActivityListener(listener);

        var builder = new EcoBuilder("activity-test-policy");
        builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(20) });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("activity-test-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);

        // Generic error
        Func<Task> actGeneric = async () =>
        {
            await executor.ExecuteAsync<string>("activity-test-policy", async ct =>
            {
                await Task.Delay(200, ct);
                return "late";
            });
        };
        await actGeneric.Should().ThrowAsync<ResilienceTimeoutException>();

        // Void error
        Func<Task> actVoid = async () =>
        {
            await executor.ExecuteAsync("activity-test-policy", async ct =>
            {
                await Task.Delay(200, ct);
            });
        };
        await actVoid.Should().ThrowAsync<ResilienceTimeoutException>();

        stoppedActivities.Count.Should().BeGreaterThanOrEqualTo(2);
        stoppedActivities.Should().AllSatisfy(a =>
        {
            a.Status.Should().Be(ActivityStatusCode.Error);
            a.StatusDescription.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitBrokenWithNullRetryAfter_LogsIndefinite()
    {
        // Arrange
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        var manualControl = new PollyCircuitBreaker.CircuitBreakerManualControl();
        pollyBuilder.AddCircuitBreaker(new PollyCircuitBreaker.CircuitBreakerStrategyOptions
        {
            ManualControl = manualControl
        });

        var pipeline = new PollyResiliencePipeline("cb-indefinite-policy", pollyBuilder.Build());
        var registry = new ResiliencePipelineRegistry();
        registry.Register("cb-indefinite-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        await manualControl.IsolateAsync();

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync("cb-indefinite-policy", ct => ValueTask.FromResult("ok"));

        // Assert
        await act.Should().ThrowAsync<CircuitBrokenException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceCircuitBroken" &&
            e.Message.Contains("RetryAfter: Indefinite"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitBrokenWithExplicitRetryAfter_LogsFormattedDuration()
    {
        // Arrange
        var pipeline = new FakeRejectingPipeline("cb-duration-policy", new CircuitBrokenException("cb-duration-policy", TimeSpan.FromSeconds(5)));
        var registry = new ResiliencePipelineRegistry();
        registry.Register("cb-duration-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync("cb-duration-policy", ct => ValueTask.FromResult("ok"));

        // Assert
        await act.Should().ThrowAsync<CircuitBrokenException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceCircuitBroken" &&
            e.Message.Contains("00:00:05"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateLimitWithExplicitRetryAfter_LogsRetryAfterDuration()
    {
        // Arrange
        var pipeline = new FakeRejectingPipeline("rl-retryafter-policy", new RateLimitRejectedException("rl-retryafter-policy", TimeSpan.FromSeconds(15)));
        var registry = new ResiliencePipelineRegistry();
        registry.Register("rl-retryafter-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync("rl-retryafter-policy", ct => ValueTask.FromResult("ok"));

        // Assert
        await act.Should().ThrowAsync<RateLimitRejectedException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceRateLimited" &&
            e.Message.Contains("RetryAfter: 00:00:15"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateLimitWithNullRetryAfter_LogsUnknown()
    {
        // Arrange
        var pipeline = new FakeRejectingPipeline("rl-null-retryafter", new RateLimitRejectedException("rl-null-retryafter", retryAfter: null));
        var registry = new ResiliencePipelineRegistry();
        registry.Register("rl-null-retryafter", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync("rl-null-retryafter", ct => ValueTask.FromResult("ok"));

        // Assert
        await act.Should().ThrowAsync<RateLimitRejectedException>();
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Warning &&
            e.EventId.Name == "ResilienceRateLimited" &&
            e.Message.Contains("RetryAfter: Unknown"));
    }

    [Fact]
    public async Task ExecuteAsync_RecordsResilienceMeterMetricsOnSuccessAndFailure()
    {
        var measurements = new List<(string Instrument, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, listener) =>
        {
            if (inst.Meter.Name == "EricksonLopez.Resilience")
            {
                listener.EnableMeasurementEvents(inst);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            measurements.Add((inst.Name, val, tags.ToArray()));
        });
        meterListener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            measurements.Add((inst.Name, val, tags.ToArray()));
        });
        meterListener.Start();

        var builder = new EcoBuilder("metric-policy");
        builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(20) });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("metric-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);
        var ctx = new ResilienceContext("metric-policy", operationName: "MetricOp", tenantId: "tenant-m");

        // Generic Success
        await executor.ExecuteAsync("metric-policy", c => ValueTask.FromResult(1), ctx);

        // Void Success
        await executor.ExecuteAsync("metric-policy", c => ValueTask.CompletedTask, ctx);

        // Generic Failure
        Func<Task> actGeneric = async () => await executor.ExecuteAsync("metric-policy", async c =>
        {
            await Task.Delay(200, c.CancellationToken);
            return 2;
        }, ctx);
        await actGeneric.Should().ThrowAsync<ResilienceTimeoutException>();

        // Void Failure
        Func<Task> actVoid = async () => await executor.ExecuteAsync("metric-policy", async c =>
        {
            await Task.Delay(200, c.CancellationToken);
        }, ctx);
        await actVoid.Should().ThrowAsync<ResilienceTimeoutException>();

        // Assert
        measurements.Count(m => m.Instrument == "resilience.execution.duration" && m.Tags.Any(t => t.Key == "resilience.status" && Equals(t.Value, "success"))).Should().Be(2);
        measurements.Count(m => m.Instrument == "resilience.execution.duration" && m.Tags.Any(t => t.Key == "resilience.status" && Equals(t.Value, "failure"))).Should().Be(2);
        measurements.Count(m => m.Instrument == "resilience.timeout.rejections").Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateLimited_RecordsRateLimitMetrics()
    {
        var measurements = new List<(string Instrument, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, listener) =>
        {
            if (inst.Meter.Name == "EricksonLopez.Resilience")
            {
                listener.EnableMeasurementEvents(inst);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((inst, val, tags, state) =>
        {
            measurements.Add((inst.Name, val, tags.ToArray()));
        });
        meterListener.SetMeasurementEventCallback<double>((inst, val, tags, state) =>
        {
            measurements.Add((inst.Name, val, tags.ToArray()));
        });
        meterListener.Start();

        var builder = new EcoBuilder("rl-metric-policy");
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = RateLimiterType.FixedWindow;
            opt.PermitLimit = 1;
            opt.QueueLimit = 0;
            opt.Window = TimeSpan.FromMinutes(1);
        });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("rl-metric-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry);
        await executor.ExecuteAsync("rl-metric-policy", ct => ValueTask.CompletedTask);

        Func<Task> act = async () => await executor.ExecuteAsync("rl-metric-policy", ct => ValueTask.CompletedTask);
        await act.Should().ThrowAsync<RateLimitRejectedException>();

        measurements.Should().Contain(m => m.Instrument == "resilience.rate_limiter.rejections");
    }

    private sealed class FakeRejectingPipeline : IResiliencePipeline
    {
        private readonly Exception _exceptionToThrow;

        public FakeRejectingPipeline(string name, Exception exceptionToThrow)
        {
            Name = name;
            _exceptionToThrow = exceptionToThrow;
        }

        public string Name { get; }

        public ValueTask<TResult> ExecuteAsync<TResult>(Func<ResilienceContext, ValueTask<TResult>> operation, ResilienceContext context, CancellationToken cancellationToken = default)
            => throw _exceptionToThrow;

        public ValueTask<TResult> ExecuteAsync<TResult>(Func<CancellationToken, ValueTask<TResult>> operation, CancellationToken cancellationToken = default)
            => throw _exceptionToThrow;

        public ValueTask ExecuteAsync(Func<ResilienceContext, ValueTask> operation, ResilienceContext context, CancellationToken cancellationToken = default)
            => throw _exceptionToThrow;

        public ValueTask ExecuteAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default)
            => throw _exceptionToThrow;
    }

    private sealed class FakeNonValidatingRegistry : IResiliencePipelineRegistry
    {
        private readonly IResiliencePipeline _dummy = new PollyResiliencePipeline("dummy", new global::Polly.ResiliencePipelineBuilder().Build());
        private readonly List<object> _registered = new();

        public void Register(string policyName, IResiliencePipeline pipeline) { _registered.Add(pipeline); }
        public void Register<TResult>(string policyName, IResiliencePipeline<TResult> pipeline) { _registered.Add(pipeline); }
        public IResiliencePipeline GetPipeline(string policyName) => _dummy;
        public IResiliencePipeline<TResult> GetPipeline<TResult>(string policyName) => throw new NotImplementedException();
        public bool TryGetPipeline(string policyName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IResiliencePipeline? pipeline) { pipeline = _dummy; return true; }
        public bool TryGetPipeline<TResult>(string policyName, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IResiliencePipeline<TResult>? pipeline) { pipeline = null; return false; }
    }

    [Fact]
    public async Task ExecuteAsync_WithoutLogger_ExecutesAndPropagatesExceptionsSilently()
    {
        // Arrange
        var builder = new EcoBuilder("nolog-fail-policy");
        builder.AddTimeout(new TimeoutStrategyOptions { Timeout = TimeSpan.FromMilliseconds(20) });
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("nolog-fail-policy", pipeline);

        var executor = new PollyResilienceExecutor(registry, logger: null);

        // Generic success
        var res = await executor.ExecuteAsync("nolog-fail-policy", ct => ValueTask.FromResult(123));
        res.Should().Be(123);

        // Void success
        await executor.ExecuteAsync("nolog-fail-policy", ct => ValueTask.CompletedTask);

        // Failure
        Func<Task> act = async () =>
        {
            await executor.ExecuteAsync<int>("nolog-fail-policy", async ct =>
            {
                await Task.Delay(200, ct);
                return 456;
            });
        };
        await act.Should().ThrowAsync<ResilienceTimeoutException>();
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_WithTraceAndDebugLogging_LogsSuccessfully()
    {
        // Arrange
        var builder = new EcoBuilder("void-log-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("void-log-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);
        var context = new ResilienceContext("void-log-policy", operationName: "ProcessJob", tenantId: "tenant-9", correlationId: "corr-9");

        // Act
        await executor.ExecuteAsync(
            "void-log-policy",
            ctx => ValueTask.CompletedTask,
            context);

        // Assert
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Trace &&
            e.EventId.Name == "ResilienceExecuting" &&
            e.Message.Contains("Executing resilient operation ProcessJob with policy void-log-policy (Tenant: tenant-9, Correlation: corr-9)"));
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Debug &&
            e.EventId.Name == "ResilienceSuccess" &&
            e.Message.Contains("Resilient operation ProcessJob with policy void-log-policy succeeded in"));
    }

    [Fact]
    public async Task ExecuteAsync_VoidOperation_WithNullTenantAndCorrelation_LogsNone()
    {
        // Arrange
        var builder = new EcoBuilder("void-none-policy");
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        var registry = new ResiliencePipelineRegistry();
        registry.Register("void-none-policy", pipeline);

        var spyLogger = new SpyResilienceLogger(minLevel: LogLevel.Trace);
        var executor = new PollyResilienceExecutor(registry, spyLogger);
        var context = new ResilienceContext("void-none-policy", operationName: "ProcessJob", tenantId: null, correlationId: null);

        // Act
        await executor.ExecuteAsync(
            "void-none-policy",
            ctx => ValueTask.CompletedTask,
            context);

        // Assert
        spyLogger.LoggedEntries.Should().Contain(e => e.Level == LogLevel.Trace &&
            e.EventId.Name == "ResilienceExecuting" &&
            e.Message.Contains("Executing resilient operation ProcessJob with policy void-none-policy (Tenant: None, Correlation: None)"));
    }

    [Fact]
    public async Task ExecuteAsync_OnFailure_WithActiveActivityAndCircuitBrokenException_SetsActivityErrorAndRecordsTelemetry()
    {
        var pipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(new EcoBuilder("test-circuit-policy"));
        var registry = new ResiliencePipelineRegistry();
        registry.Register("test-circuit-policy", pipeline);
        var executor = new PollyResilienceExecutor(registry);

        Activity? capturedActivity = null;
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "EricksonLopez.Resilience",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => capturedActivity = a
        };
        ActivitySource.AddActivityListener(activityListener);

        long cbRejections = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "EricksonLopez.Resilience" && instrument.Name == "resilience.circuit_breaker.rejections")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            Interlocked.Add(ref cbRejections, measurement);
        });
        meterListener.Start();

        var context = ResilienceContext.Create("test-circuit-policy")
            .WithOperationName("ExecuteWithCircuitBreaker")
            .WithTenantId("tenant-123");

        var act = async () => await executor.ExecuteAsync<string>("test-circuit-policy", async _ =>
        {
            await Task.Yield();
            throw new CircuitBrokenException("Circuit is open", TimeSpan.FromSeconds(5));
        }, context);

        var thrown = await act.Should().ThrowAsync<CircuitBrokenException>();

        capturedActivity.Should().NotBeNull();
        capturedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        capturedActivity.StatusDescription.Should().Be(thrown.Which.Message);
        capturedActivity.Events.Should().Contain(e => e.Name == "exception");

        meterListener.RecordObservableInstruments();
        cbRejections.Should().Be(1);
    }

    private sealed class SpyResilienceLogger : ILogger<PollyResilienceExecutor>
    {
        private readonly LogLevel _minLevel;

        public SpyResilienceLogger(LogLevel minLevel = LogLevel.Trace)
        {
            _minLevel = minLevel;
        }

        public List<(LogLevel Level, EventId EventId, string Message)> LoggedEntries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel && _minLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                LoggedEntries.Add((logLevel, eventId, formatter(state, exception)));
            }
        }
    }
}
