namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Artifacts;

public sealed class ArtifactGenerator : IArtifactGenerator
{
    public GeneratedArtifact Create(ArtifactType type, string title, string content, string source)
    {
        return new GeneratedArtifact(
            Id: Guid.NewGuid(),
            Type: type,
            Title: string.IsNullOrWhiteSpace(title) ? type.ToString() : title.Trim(),
            Content: content ?? string.Empty,
            CreatedAtUtc: DateTime.UtcNow,
            Source: string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim());
    }
}
