using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Libr4.AI.Infrastructure.AI;
using Libr4.AI.Infrastructure.AI.Providers;

namespace Libr4.AI.Tests;

public class AITest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("AI INTEGRATION TEST SUITE");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var apiKey = configuration["AI:OpenRouter:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("❌ API key not found in configuration");
            return;
        }

        Console.WriteLine($"✅ API key loaded (length: {apiKey.Length})");
        Console.WriteLine($"Model: {configuration["AI:OpenRouter:DefaultModel"]}");
        Console.WriteLine();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<AITest>();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(loggerFactory);
        services.AddHttpClient<OpenRouterProvider>();
        services.AddSingleton<AIProviderFactory>();
        services.AddScoped<IAIService, AIService>();

        var serviceProvider = services.BuildServiceProvider();
        var aiService = serviceProvider.GetRequiredService<IAIService>();

        Console.WriteLine("✅ AI service created successfully");
        Console.WriteLine();

        var passed = 0;
        var total = 0;

        // Test 1: Basic AI Connection
        Console.WriteLine("📝 TEST 1: Basic AI Connection");
        total++;
        try
        {
            var response = await aiService.ChatAsync("Hello, respond with just 'OK'", "You are a helpful assistant.");
            Console.WriteLine($"AI Response: {response}");
            if (response.Contains("OK"))
            {
                passed++;
                Console.WriteLine("✅ Basic AI connection test PASSED");
            }
            else
            {
                Console.WriteLine("❌ Basic AI connection test FAILED: Unexpected response");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Basic AI connection test FAILED: {ex.Message}");
        }
        Console.WriteLine();

        // Test 2: AI Text Analysis - Complexity
        Console.WriteLine("📝 TEST 2: AI Text Analysis - Complexity");
        total++;
        try
        {
            var analysis = await aiService.AnalyzeTextAsync("Build a REST API for task management with authentication and authorization", "complexity");
            Console.WriteLine($"Analysis result: {analysis}");
            if (analysis.Contains("complexity") || analysis.Contains("score"))
            {
                passed++;
                Console.WriteLine("✅ AI complexity analysis test PASSED");
            }
            else
            {
                Console.WriteLine("❌ AI complexity analysis test FAILED: Unexpected response");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AI complexity analysis test FAILED: {ex.Message}");
        }
        Console.WriteLine();

        // Test 3: AI Text Analysis - Skills
        Console.WriteLine("📝 TEST 3: AI Text Analysis - Skills");
        total++;
        try
        {
            var analysis = await aiService.AnalyzeTextAsync("The task requires knowledge of C#, ASP.NET Core, PostgreSQL, and REST API design", "skills");
            Console.WriteLine($"Analysis result: {analysis}");
            if (analysis.Contains("skill") || analysis.Contains("C#"))
            {
                passed++;
                Console.WriteLine("✅ AI skills analysis test PASSED");
            }
            else
            {
                Console.WriteLine("❌ AI skills analysis test FAILED: Unexpected response");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AI skills analysis test FAILED: {ex.Message}");
        }
        Console.WriteLine();

        // Test 4: AI Text Analysis - Risk
        Console.WriteLine("📝 TEST 4: AI Text Analysis - Risk");
        total++;
        try
        {
            var analysis = await aiService.AnalyzeTextAsync("Build a REST API for task management with budget $5000 and deadline 30 days", "risk");
            Console.WriteLine($"Analysis result: {analysis}");
            if (analysis.Contains("risk") || analysis.Contains("severity"))
            {
                passed++;
                Console.WriteLine("✅ AI risk analysis test PASSED");
            }
            else
            {
                Console.WriteLine("❌ AI risk analysis test FAILED: Unexpected response");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ AI risk analysis test FAILED: {ex.Message}");
        }
        Console.WriteLine();

        // Summary
        Console.WriteLine("========================================");
        Console.WriteLine("TEST SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine($"Passed: {passed}/{total}");
        
        if (passed == total)
        {
            Console.WriteLine();
            Console.WriteLine("🎉 ALL TESTS PASSED!");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine($"⚠️  {total - passed} test(s) failed");
        }
    }
}
