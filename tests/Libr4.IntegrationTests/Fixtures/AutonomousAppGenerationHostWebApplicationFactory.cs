using Libr4.IDE.AutonomousAppGeneration.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Libr4.IntegrationTests.Fixtures;

public sealed class AutonomousAppGenerationHostWebApplicationFactory
    : WebApplicationFactory<AutonomousAppGenerationHostWebApplicationFactoryAnchor>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AutonomousAppGeneration:AllowProcessFallback"] = "true",
                ["AutonomousAppGeneration:RuntimeProvider"] = "process",
                ["AutonomousAppGeneration:BenchmarkMode:EnableBenchmarkMode"] = "false",
                ["OpenRouter:ApiKey"] = "test-key",
                ["OpenRouter:Endpoint"] = "https://openrouter.ai/api/v1",
            });
        });

        // Prevent Serilog from terminating the test host on startup warnings.
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = false;
            options.ValidateOnBuild = false;
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        return base.CreateHost(builder);
    }
}
