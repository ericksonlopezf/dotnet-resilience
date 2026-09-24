// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;

namespace EricksonLopez.Resilience.Showcase.Levels;

/// <summary>
/// Provides conceptual demonstrations illustrating resilience fundamentals, ecosystem invariants, and architectural philosophy.
/// </summary>
public static class Level0Conceptual
{
    /// <summary>
    /// Executes the conceptual resilience demonstration.
    /// </summary>
    /// <returns>A value task representing the asynchronous operation.</returns>
    public static ValueTask RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine(" [LEVEL 0] CONCEPTUAL FOUNDATIONS OF ERICKSONLOPEZ.RESILIENCE");
        Console.WriteLine("================================================================================");
        Console.WriteLine();
        Console.WriteLine(" 1. WHAT IS ERICKSONLOPEZ.RESILIENCE?");
        Console.WriteLine("    An enterprise architectural fault-tolerance and resilience framework for .NET 10");
        Console.WriteLine("    designed under Clean Architecture, DDD, and Native AOT / Trimming compatibility.");
        Console.WriteLine();
        Console.WriteLine(" 2. WHAT PROBLEM DOES IT SOLVE?");
        Console.WriteLine("    In distributed systems, network glitches, transient outages, load spikes, and");
        Console.WriteLine("    unpredicted latencies are inevitable. Coupling business logic to infrastructure");
        Console.WriteLine("    libraries (like raw Polly) pollutes the domain and impedes maintenance,");
        Console.WriteLine("    testing, and architectural evolution.");
        Console.WriteLine();
        Console.WriteLine(" 3. CORE ECOSYSTEM INVARIANT");
        Console.WriteLine("    Polly v8 is strictly an internal infrastructure execution detail (L4 layer).");
        Console.WriteLine("    Domain and Application layers interact EXCLUSIVELY through first-party");
        Console.WriteLine("    abstractions (IResilienceExecutor, IResiliencePipeline, ResilienceContext).");
        Console.WriteLine();
        Console.WriteLine(" 4. ARCHITECTURAL COMPARISON:");
        Console.WriteLine("    +-------------------------+-------------------------+-----------------------------+");
        Console.WriteLine("    | Dimension               | Raw Polly Direct        | EricksonLopez.Resilience    |");
        Console.WriteLine("    +-------------------------+-------------------------+-----------------------------+");
        Console.WriteLine("    | Domain Coupling         | High (Package leakage)  | Zero (L0 Pure Abstractions) |");
        Console.WriteLine("    | Result<T> Evaluation    | Manual / Imperative     | Native / Deterministic      |");
        Console.WriteLine("    | Transactions and UoW    | Dirty state risk        | Per-attempt transactions    |");
        Console.WriteLine("    | Idempotency             | No unified context      | Keys preserved across retry |");
        Console.WriteLine("    | Native AOT / Trimming   | Variable by version     | 100% Trim-Safe verified     |");
        Console.WriteLine("    | OpenTelemetry           | Scattered config        | Standard metrics and spans  |");
        Console.WriteLine("    +-------------------------+-------------------------+-----------------------------+");
        Console.WriteLine();
        Console.WriteLine(" [✓] Level 0 Completed successfully.");
        Console.WriteLine("--------------------------------------------------------------------------------\n");
        return ValueTask.CompletedTask;
    }
}
