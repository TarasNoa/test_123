namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Artifacts;

public enum ArtifactType
{
    TaskList,
    Plan,
    Screenshot,
    Recording,
    Report
}

public sealed record GeneratedArtifact(
    Guid Id,
    ArtifactType Type,
    string Title,
    string Content,
    DateTime CreatedAtUtc,
    string Source);
