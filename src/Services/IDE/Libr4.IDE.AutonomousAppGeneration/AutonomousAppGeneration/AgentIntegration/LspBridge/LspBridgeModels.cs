namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.LspBridge;

public sealed record LspDiagnostic(
    string FilePath,
    int Line,
    int Column,
    string Severity,
    string Message,
    string? Code,
    string Source);

public sealed record LspLocation(
    string FilePath,
    int Line,
    int Column,
    string? Symbol);

public sealed record LspWorkspaceContext(
    IReadOnlyList<LspDiagnostic> Diagnostics,
    IReadOnlyList<LspLocation> Definitions,
    IReadOnlyList<LspLocation> References)
{
    public static LspWorkspaceContext Empty { get; } =
        new(Array.Empty<LspDiagnostic>(), Array.Empty<LspLocation>(), Array.Empty<LspLocation>());

    public string FormatForContextPack(int maxChars = 4000)
    {
        if (Diagnostics.Count == 0 && Definitions.Count == 0 && References.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        if (Diagnostics.Count > 0)
        {
            sb.AppendLine("## lsp_diagnostics");
            foreach (var d in Diagnostics.Take(24))
                sb.AppendLine($"- [{d.Severity}] {d.FilePath}:{d.Line}:{d.Column} {d.Message} ({d.Source})");
        }

        if (Definitions.Count > 0)
        {
            sb.AppendLine("## lsp_definitions");
            foreach (var d in Definitions.Take(12))
                sb.AppendLine($"- {d.Symbol ?? "?"} @ {d.FilePath}:{d.Line}");
        }

        if (References.Count > 0)
        {
            sb.AppendLine("## lsp_references");
            foreach (var r in References.Take(12))
                sb.AppendLine($"- {r.Symbol ?? "?"} @ {r.FilePath}:{r.Line}");
        }

        var text = sb.ToString().TrimEnd();
        return text.Length <= maxChars ? text : text[..maxChars] + "\n…";
    }
}
