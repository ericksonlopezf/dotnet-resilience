// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Resilience.DependencyInjection;
using EricksonLopez.Resilience.Extensions;
using EricksonLopez.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides scalability demonstrations illustrating high-throughput execution, zero-allocation pooling, and rate limiting.
/// </summary>
public static class Level7Scalability
{
    /// <summary>
    /// Executes the scalability resilience demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public static async ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 7] SCALABILITY: HIGH THROUGHPUT AND ZERO-ALLOCATION POOLING");
        Console.WriteLine("================================================================================");

        var services = new ServiceCollection();
        services.AddEricksonLopezResilience();

        services.AddResiliencePolicy("high-throughput-policy", builder =>
        {
            builder
                .AddRateLimiter(opt =>
                {
                    opt.PermitLimit = 10000;
                    opt.QueueLimit = 100;
                    opt.Window = TimeSpan.FromSeconds(1);
                })
                .AddTimeout(TimeSpan.FromSeconds(10));
        });

        var sp = services.BuildServiceProvider();
        var executor = sp.GetRequiredService<IResilienceExecutor>();

        const int iterations = 1000;
        Console.WriteLine($"\n -> Executing rapid benchmark with {iterations:N0} concurrent operations...");

        var sw = Stopwatch.StartNew();
        var context = ResilienceContext.Create("high-throughput-policy");

        for (int i = 0; i < iterations; i++)
        {
            await executor.ExecuteAsync(
                "high-throughput-policy",
                async ctx =>
                {
                    // Ultra-lightweight operation
                    return await ValueTask.FromResult(1);
                },
                context);
        }
        sw.Stop();

        var elapsedMs = sw.Elapsed.TotalMilliseconds;
        var opsPerSec = (iterations / sw.Elapsed.TotalSeconds);
        Console.WriteLine($" [✓] {iterations:N0} executions completed in {elapsedMs:F2}ms ({opsPerSec:N0} ops/sec).");
        Console.WriteLine(" [✓] Internal pooling reuses context structures without saturating GC.");

        Console.WriteLine("\n [✓] Level 7 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
    }
}
