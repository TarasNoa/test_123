namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class BenchmarkExportOptions
{
    public string ExportRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "libr4-autogen-benchmark-exports");
    public int RetentionHours { get; set; } = 24;
    public int MaxArtifacts { get; set; } = 200;
}
