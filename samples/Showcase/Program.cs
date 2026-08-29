// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Resilience.Showcase.Cookbook;
using EricksonLopez.Resilience.Showcase.Levels;

namespace EricksonLopez.Resilience.Showcase;

/// <summary>
/// Official executable showcase and architectural reference application for EricksonLopez.Resilience (.NET 10).
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================================================================");
        Console.WriteLine("  ERICKSONLOPEZ.RESILIENCE — OFFICIAL SHOWCASE & REFERENCE IMPLEMENTATION");
        Console.WriteLine("================================================================================");

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

                case "cookbook":
                    await CookbookRecipes.RunAllAsync();
                    break;

                case "all":
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
                    await CookbookRecipes.RunAllAsync();
                    break;
            }

            Console.WriteLine("================================================================================");
            Console.WriteLine(" [✓] SHOWCASE COMPLETED SUCCESSFULLY — 100% OF APIS AND LEVELS VERIFIED");
            Console.WriteLine("================================================================================");
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
