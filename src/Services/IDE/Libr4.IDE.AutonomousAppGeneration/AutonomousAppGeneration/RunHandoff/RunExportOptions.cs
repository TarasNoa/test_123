namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed class RunExportOptions
{
    public const string SectionName = "AutonomousAppGeneration:RunExport";

    public string ExportRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "libr4-run-exports");

    public long MaxBundleBytes { get; set; } = 2L * 1024 * 1024 * 1024;

    public int RetentionDays { get; set; } = 7;

    public int MaxArtifacts { get; set; } = 200;

    public string[] WorkspaceExcludeDirNames { get; set; } =
    [
        "node_modules",
        ".venv",
        "venv",
        "__pycache__",
        ".git"
    ];
}
