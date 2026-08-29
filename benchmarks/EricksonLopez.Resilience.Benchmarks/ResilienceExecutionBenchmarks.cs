// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using EricksonLopez.Resilience.Classification;
using EricksonLopez.Resilience.Polly.Adapters;
using EricksonLopez.Resilience.Polly.Builders;
using EricksonLopez.Resilience.Registry;
using EricksonLopez.Result;
using global::Polly;
using global::Polly.Timeout;

namespace EricksonLopez.Resilience.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public sealed class ResilienceExecutionBenchmarks
{
    private const string PolicyName = "benchmark-policy";

    private IResiliencePipeline _ecosystemPipeline = null!;
    private global::Polly.ResiliencePipeline _rawPollyPipeline = null!;
    private PollyResilienceExecutor _executor = null!;
    private ResilienceContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        // 1. Raw Polly Pipeline
        var pollyBuilder = new global::Polly.ResiliencePipelineBuilder();
        pollyBuilder.AddTimeout(new global::Polly.Timeout.TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30)
        });
        _rawPollyPipeline = pollyBuilder.Build();

        // 2. Ecosystem Pipeline Builder
        var builder = new EricksonLopez.Resilience.Builder.ResiliencePipelineBuilder(PolicyName);
        builder.AddTimeout(opt => opt.Timeout = TimeSpan.FromSeconds(30));
        _ecosystemPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);

        // 3. Registry and Executor
        var registry = new ResiliencePipelineRegistry();
        registry.Register(PolicyName, _ecosystemPipeline);
        _executor = new PollyResilienceExecutor(registry);

        _context = ResilienceContext.Create(PolicyName, CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<int> DirectPollyExecution()
    {
        return await _rawPollyPipeline.ExecuteAsync(static async (state, ct) =>
        {
            return await ValueTask.FromResult(42);
        }, 0, CancellationToken.None);
    }

    [Benchmark]
    public async ValueTask<int> EcosystemPipelineExecution()
    {
        return await _ecosystemPipeline.ExecuteAsync(static async ctx =>
        {
            return await ValueTask.FromResult(42);
        }, _context, CancellationToken.None);
    }

    [Benchmark]
    public async ValueTask<int> EcosystemExecutorExecution()
    {
        return await _executor.ExecuteAsync(
            PolicyName,
            static async ctx => await ValueTask.FromResult(42),
            _context,
            CancellationToken.None);
    }

    [Benchmark]
    public async ValueTask<Result<int>> ResultResilientExecution()
    {
        return await _ecosystemPipeline.ExecuteAsync(static async ctx =>
        {
            return await ValueTask.FromResult(Result<int>.Success(42));
        }, _context, CancellationToken.None);
    }
}
