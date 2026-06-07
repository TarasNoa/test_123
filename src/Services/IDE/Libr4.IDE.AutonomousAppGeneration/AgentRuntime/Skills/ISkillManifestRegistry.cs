namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;

public interface ISkillManifestRegistry
{
    IReadOnlyList<SkillManifestEntry> List();

    SkillManifestEntry? Find(string skillName);

    string FormatManifest();

    Task<string> LoadContentAsync(string skillName, CancellationToken ct = default);
}
