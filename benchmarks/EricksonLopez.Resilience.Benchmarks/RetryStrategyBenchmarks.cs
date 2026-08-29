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
public class RetryStrategyBenchmarks
{
    private IResiliencePipeline _retryPipeline = null!;
    private ResilienceContext _context = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new ResiliencePipelineBuilder("retry-benchmarks");
        builder.AddRetry(opt =>
        {
            opt.MaxRetryAttempts = 3;
            opt.BackoffType = BackoffType.ExponentialWithJitter;
            opt.Delay = TimeSpan.FromMilliseconds(10);
        });

        _retryPipeline = PollyPipelineBuilderTranslator.TranslateAndBuild(builder);
        _context = ResilienceContext.Create("retry-benchmarks", CancellationToken.None);
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<int> SuccessfulPathWithRetryPolicy()
    {
        return await _retryPipeline.ExecuteAsync(
            static async ctx => await ValueTask.FromResult(300),
            _context,
            CancellationToken.None);
    }
}
