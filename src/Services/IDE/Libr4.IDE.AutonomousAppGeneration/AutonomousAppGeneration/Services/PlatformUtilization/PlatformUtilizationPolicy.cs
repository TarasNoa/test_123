namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

/// <summary>Central policy: benchmark shortcuts vs full platform utilization.</summary>
public static class PlatformUtilizationPolicy
{
    public static bool IsBenchmarkShortcutPathActive(
        AutonomousBenchmarkModeOptions benchmark,
        AutonomousPlatformUtilizationOptions platform) =>
        benchmark.EnableBenchmarkMode
        && benchmark.UseBenchmarkExecutionPath
        && !platform.EnableFullPlatformUtilization;

    public static bool ShouldRelaxBenchmarkGate(
        AutonomousBenchmarkModeOptions benchmark,
        AutonomousPlatformUtilizationOptions platform,
        bool relaxWhenBenchmark) =>
        benchmark.EnableBenchmarkMode
        && relaxWhenBenchmark
        && !platform.EnableFullPlatformUtilization;
}
