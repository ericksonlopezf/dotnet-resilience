// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using EricksonLopez.Resilience.Builder;
using EricksonLopez.Resilience.Options;
using EricksonLopez.Resilience.Polly.Builders;

namespace EricksonLopez.Resilience.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class RateLimiterBenchmarks
{
    private IResiliencePipeline _rateLimiterPipeline = null!;
    private ResilienceContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new ResiliencePipelineBuilder("rate-limiter-benchmarks");
        builder.AddRateLimiter(opt =>
        {
            opt.LimiterType = RateLimiterType.Concurrency;
            opt.PermitLimit = 10000;
            opt.QueueLimit = 10000;
        });

        _rateLimiterPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        _context = ResilienceContext.Create("rate-limiter-benchmarks", CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<int> PermittedExecution()
    {
        return await _rateLimiterPipeline.ExecuteAsync(
            static async ctx => await ValueTask.FromResult(200),
            _context,
            CancellationToken.None);
    }
}
