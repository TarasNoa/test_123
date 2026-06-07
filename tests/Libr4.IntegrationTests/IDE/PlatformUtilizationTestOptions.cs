using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Microsoft.Extensions.Options;

namespace Libr4.IntegrationTests.IDE;

internal static class PlatformUtilizationTestOptions
{
    internal static IOptions<AutonomousPlatformUtilizationOptions> Production =>
        Options.Create(new AutonomousPlatformUtilizationOptions());

    internal static IOptions<AutonomousPlatformUtilizationOptions> BenchmarkShortcuts =>
        Options.Create(new AutonomousPlatformUtilizationOptions
        {
            EnableFullPlatformUtilization = false
        });
}
