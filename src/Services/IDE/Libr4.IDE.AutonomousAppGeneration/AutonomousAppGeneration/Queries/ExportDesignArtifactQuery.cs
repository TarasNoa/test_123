namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record ExportDesignArtifactQuery(
    string ArtifactId,
    string? ExportPath = null);

public sealed record ExportDesignArtifactResult(
    string ArtifactId,
    string ExportPath,
    string ContentHash,
    int PayloadBytes,
    DateTime ExportedAtUtc);
