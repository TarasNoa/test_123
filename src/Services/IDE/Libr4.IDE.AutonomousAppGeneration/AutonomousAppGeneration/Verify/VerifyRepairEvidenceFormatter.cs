using System.Text;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public static class VerifyRepairEvidenceFormatter
{
    public static string EnrichWithArtifactPaths(string baseText, Guid runId, IVerifyEvidenceStore evidenceStore)
    {
        var bundle = evidenceStore.List(runId);
        if (!bundle.DirectoryExists || bundle.Artifacts.Count == 0)
            return baseText;

        var sb = new StringBuilder(baseText.TrimEnd());
        sb.AppendLine();
        sb.AppendLine("verify_artifacts:");

        AppendIfPresent(sb, bundle, VerifyEvidenceKind.Screenshot, "screenshot_evidence");
        AppendIfPresent(sb, bundle, VerifyEvidenceKind.ConsoleErrors, "console_errors_evidence");
        AppendIfPresent(sb, bundle, VerifyEvidenceKind.DomSnapshot, "dom_snapshot_evidence");
        AppendIfPresent(sb, bundle, VerifyEvidenceKind.SmokeVideo, "smoke_video_evidence");

        return sb.ToString().TrimEnd();
    }

    private static void AppendIfPresent(
        StringBuilder sb,
        VerifyEvidenceBundle bundle,
        VerifyEvidenceKind kind,
        string label)
    {
        var artifact = bundle.Artifacts.FirstOrDefault(a => a.Kind == kind);
        if (artifact is null)
            return;

        sb.AppendLine($"{label}={artifact.AbsolutePath}");
        sb.AppendLine($"{label}_url={artifact.DownloadUrl}");
    }
}
