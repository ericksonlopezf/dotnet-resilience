// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Polly.Builders;

namespace EricksonLopez.Resilience.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class CircuitBreakerBenchmarks
{
    private IResiliencePipeline _closedCircuitPipeline = null!;
    private ResilienceContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new ResiliencePipelineBuilder("circuit-benchmarks");
        builder.AddCircuitBreaker(opt =>
        {
            opt.SamplingDuration = TimeSpan.FromSeconds(10);
            opt.FailureRatio = 0.5;
            opt.MinimumThroughput = 100;
            opt.BreakDuration = TimeSpan.FromMinutes(1);
        });

        _closedCircuitPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        _context = ResilienceContext.Create("circuit-benchmarks", CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<int> ClosedCircuitHealthyExecution()
    {
        return await _closedCircuitPipeline.ExecuteAsync(
            static async ctx => await ValueTask.FromResult(100),
            _context,
            CancellationToken.None);
    }
}
