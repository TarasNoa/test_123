namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Artifacts;

public interface IArtifactGenerator
{
    GeneratedArtifact Create(ArtifactType type, string title, string content, string source);
}
