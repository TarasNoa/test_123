namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;

public sealed class ContextFragmentOptions
{
    public int MaxTotalChars { get; set; } = 24_000;

    public Dictionary<string, int> PerTypeCaps { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["build_log"] = 12_000,
        ["error_report"] = 6_000,
        ["file_excerpt"] = 8_000,
        ["design_artifact"] = 4_000,
        ["verify_evidence"] = 4_000,
        ["git_diff"] = 8_000,
        ["lsp_diagnostics"] = 3_500,
        ["fast_context"] = 6_000
    };

    public int GetCap(ContextFragmentType type) =>
        PerTypeCaps.TryGetValue(ToKey(type), out var cap) ? cap : 4_000;

    internal static string ToKey(ContextFragmentType type) => type switch
    {
        ContextFragmentType.BuildLog => "build_log",
        ContextFragmentType.ErrorReport => "error_report",
        ContextFragmentType.FileExcerpt => "file_excerpt",
        ContextFragmentType.DesignArtifact => "design_artifact",
        ContextFragmentType.VerifyEvidence => "verify_evidence",
        ContextFragmentType.GitDiff => "git_diff",
        ContextFragmentType.LspDiagnostics => "lsp_diagnostics",
        ContextFragmentType.FastContext => "fast_context",
        _ => type.ToString().ToLowerInvariant()
    };
}
