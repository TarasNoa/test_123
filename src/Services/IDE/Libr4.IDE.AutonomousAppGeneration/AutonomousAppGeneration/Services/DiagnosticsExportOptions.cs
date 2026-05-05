namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class DiagnosticsExportOptions
{
    public string ExportRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "libr4-autogen-diagnostics-exports");
    public int RetentionHours { get; set; } = 72;
    public int MaxArtifacts { get; set; } = 300;
}
