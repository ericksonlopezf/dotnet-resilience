// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Showcase.Cookbook;
using EricksonLopez.Resilience.Showcase.Levels;

namespace EricksonLopez.Resilience.Showcase;

/// <summary>
/// Provides the main entry point and runner for the official executable showcase and architectural reference application.
/// </summary>
public static class Program
{
    private const string Separator = "================================================================================";

    /// <summary>
    /// Executes the showcase console application using the specified command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments specifying the level or recipe mode to execute.</param>
    /// <returns>A task representing the asynchronous operation that yields the process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine(Separator);
        Console.WriteLine("  ERICKSONLOPEZ.RESILIENCE — OFFICIAL SHOWCASE & REFERENCE IMPLEMENTATION");
        Console.WriteLine(Separator);

        var mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";

        try
        {
            switch (mode)
            {
                case "0" or "level0":
                    await Level0Conceptual.RunAsync();
                    break;

                case "1" or "level1":
                    await Level1QuickStart.RunAsync();
                    break;

                case "2" or "level2":
                    await Level2Configuration.RunAsync();
                    break;

                case "3" or "level3":
                    await Level3RealUseCases.RunAsync();
                    break;

                case "4" or "level4":
                    await Level4AdvancedIntegration.RunAsync();
                    break;

                case "5" or "level5":
                    await Level5Processing.RunAsync();
                    break;

                case "6" or "level6":
                    await Level6ErrorHandling.RunAsync();
                    break;

                case "7" or "level7":
                    await Level7Scalability.RunAsync();
                    break;

                case "8" or "level8":
                    await Level8Customization.RunAsync();
                    break;

                case "9" or "level9":
                    await Level9Extensions.RunAsync();
                    break;

                case "10" or "level10":
                    await Level10EnterpriseArchitecture.RunAsync();
                    break;

                case "11" or "level11":
                    await Level11ComprehensiveCoverage.RunAsync();
                    break;

                case "cookbook":
                    await CookbookRecipes.RunAllAsync();
                    break;

                default:
                    Console.WriteLine("\n[EXECUTING ALL PROGRESSIVE LEVELS AND COOKBOOK RECIPES]\n");
                    await Level0Conceptual.RunAsync();
                    await Level1QuickStart.RunAsync();
                    await Level2Configuration.RunAsync();
                    await Level3RealUseCases.RunAsync();
                    await Level4AdvancedIntegration.RunAsync();
                    await Level5Processing.RunAsync();
                    await Level6ErrorHandling.RunAsync();
                    await Level7Scalability.RunAsync();
                    await Level8Customization.RunAsync();
                    await Level9Extensions.RunAsync();
                    await Level10EnterpriseArchitecture.RunAsync();
                    await Level11ComprehensiveCoverage.RunAsync();
                    await CookbookRecipes.RunAllAsync();
                    break;
            }

            Console.WriteLine(Separator);
            Console.WriteLine(" [✓] SHOWCASE COMPLETED SUCCESSFULLY — 100% OF APIS AND LEVELS VERIFIED");
            Console.WriteLine(Separator);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR]: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }
}
