using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public sealed class ExtensionAwareSkillManifestRegistry : ISkillManifestRegistry, ISkillManifestRegistryRefresh
{
    private readonly ISkillManifestRegistry _inner;
    private readonly IExtensionHost _host;

    public ExtensionAwareSkillManifestRegistry(ISkillManifestRegistry inner, IExtensionHost host)
    {
        _inner = inner;
        _host = host;
    }

    public IReadOnlyList<SkillManifestEntry> List()
    {
        var entries = _inner.List().ToList();
        foreach (var skill in _host.Skills)
        {
            var description = string.IsNullOrWhiteSpace(skill.Definition.Description)
                ? skill.Definition.Id
                : skill.Definition.Description!;
            entries.Add(new SkillManifestEntry(skill.Definition.Id, description, skill.SkillFilePath));
        }

        return entries
            .GroupBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public SkillManifestEntry? Find(string skillName)
    {
        var builtIn = _inner.Find(skillName);
        if (builtIn is not null)
            return builtIn;

        var normalized = skillName.Trim().ToLowerInvariant().Replace(' ', '-');
        var skill = _host.Skills.FirstOrDefault(s =>
            string.Equals(s.Definition.Id, normalized, StringComparison.OrdinalIgnoreCase));
        if (skill is null)
            return null;

        var description = string.IsNullOrWhiteSpace(skill.Definition.Description)
            ? skill.Definition.Id
            : skill.Definition.Description!;
        return new SkillManifestEntry(skill.Definition.Id, description, skill.SkillFilePath);
    }

    public string FormatManifest()
    {
        var entries = List();
        if (entries.Count == 0)
            return "(no skills indexed — use activate_skill after deployment)";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Skills manifest ({entries.Count} available — use activate_skill to load full SKILL.md):");
        foreach (var entry in entries)
            sb.Append("- ").Append(entry.Id).Append(": ").AppendLine(TruncateOneLiner(entry.Description, 120));
        return sb.ToString().TrimEnd();
    }

    public async Task<string> LoadContentAsync(string skillName, CancellationToken ct = default)
    {
        var extensionSkill = Find(skillName);
        if (extensionSkill is null)
            return await _inner.LoadContentAsync(skillName, ct).ConfigureAwait(false);

        if (!File.Exists(extensionSkill.FilePath))
            return string.Empty;

        return await File.ReadAllTextAsync(extensionSkill.FilePath, ct).ConfigureAwait(false);
    }

    public void Refresh()
    {
        if (_inner is ISkillManifestRegistryRefresh refresh)
            refresh.Refresh();
    }

    private static string TruncateOneLiner(string text, int max)
    {
        var oneLine = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return oneLine.Length <= max ? oneLine : oneLine[..max] + "…";
    }
}
