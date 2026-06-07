namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public enum VerifyEvidenceKind
{
    AppLog,
    Readiness,
    Screenshot,
    SmokeVideo,
    DomSnapshot,
    ConsoleErrors,
    VerifyReport,
    Manifest,
    FailureEvidence,
    Other
}

public sealed record VerifyEvidenceArtifact(
    VerifyEvidenceKind Kind,
    string FileName,
    string RelativePath,
    string AbsolutePath,
    long SizeBytes,
    DateTime LastModifiedUtc,
    string? ContentType,
    string DownloadUrl,
    string? ThumbnailUrl);

public sealed record VerifyEvidenceBundle(
    Guid RunId,
    string EvidenceDirectory,
    bool DirectoryExists,
    string? ThumbnailUrl,
    IReadOnlyList<VerifyEvidenceArtifact> Artifacts);
