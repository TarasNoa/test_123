namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public interface IVerifyEvidenceStore
{
    string GetEvidenceDirectory(Guid runId);

    VerifyEvidenceBundle List(Guid runId);

    VerifyEvidenceArtifact? TryGet(Guid runId, string fileName);

    Task<VerifyEvidenceArtifact> PersistAsync(
        Guid runId,
        VerifyEvidenceKind kind,
        Stream content,
        string? fileName = null,
        CancellationToken ct = default);

    Task<VerifyEvidenceArtifact> PersistFromPathAsync(
        Guid runId,
        VerifyEvidenceKind kind,
        string sourcePath,
        CancellationToken ct = default);
}
