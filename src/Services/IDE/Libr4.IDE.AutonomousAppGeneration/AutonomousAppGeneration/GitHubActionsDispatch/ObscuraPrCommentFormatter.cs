using Libr4.IDE.Application.Obscura;

namespace Libr4.IDE.Application.AutonomousAppGeneration.GitHubActionsDispatch;

public static class ObscuraPrCommentFormatter
{
    public static string? BuildCommentBody(
        Guid runId,
        IReadOnlyList<ObscuraEvidenceArtifact> artifacts,
        string? publicApiBaseUrl = null)
    {
        if (artifacts.Count == 0)
            return null;

        var lines = new List<string>
        {
            "## Obscura verify evidence",
            string.Empty,
            $"Run `{runId:D}` · {artifacts.Count} artifact(s)",
            string.Empty,
            "| Step | Kind | Name | Size | Link |",
            "| --- | --- | --- | ---: | --- |"
        };

        foreach (var artifact in artifacts.OrderBy(a => a.StepNumber ?? int.MaxValue).ThenBy(a => a.FileName))
        {
            var link = ResolveArtifactLink(runId, artifact, publicApiBaseUrl);
            var linkCell = string.IsNullOrWhiteSpace(link) ? "—" : $"[{artifact.FileName}]({link})";
            lines.Add(
                $"| {artifact.StepNumber?.ToString() ?? "—"} | {artifact.Kind} | {artifact.LogicalName ?? artifact.FileName} | {artifact.SizeBytes} | {linkCell} |");
        }

        var screenshots = artifacts
            .Where(a => a.Kind == ObscuraEvidenceKind.Screenshot)
            .OrderBy(a => a.StepNumber ?? int.MaxValue)
            .Take(6)
            .ToList();

        if (screenshots.Count > 0 && !string.IsNullOrWhiteSpace(publicApiBaseUrl))
        {
            lines.Add(string.Empty);
            lines.Add("### Screenshots");
            foreach (var shot in screenshots)
            {
                var url = ResolveArtifactLink(runId, shot, publicApiBaseUrl);
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                var label = shot.LogicalName ?? shot.FileName;
                lines.Add($"![{label}]({url})");
            }
        }

        lines.Add(string.Empty);
        lines.Add("_Posted automatically by Libr4 ship stage._");
        return string.Join('\n', lines);
    }

    private static string? ResolveArtifactLink(
        Guid runId,
        ObscuraEvidenceArtifact artifact,
        string? publicApiBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(artifact.DownloadUrl) && artifact.DownloadUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return artifact.DownloadUrl;

        if (string.IsNullOrWhiteSpace(publicApiBaseUrl))
            return null;

        var baseUrl = publicApiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/api/v1/ide/app-generation/{runId:D}/obscura/artifacts/{Uri.EscapeDataString(artifact.FileName)}";
    }
}
