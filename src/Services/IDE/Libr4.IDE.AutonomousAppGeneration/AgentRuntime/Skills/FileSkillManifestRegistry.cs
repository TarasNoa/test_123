using Libr4.IDE.AutonomousAppGeneration.Agents;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;

public sealed class FileSkillManifestRegistry : ISkillManifestRegistry, ISkillManifestRegistryRefresh
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["django-rest-framework"] = "python-django",
        ["drf"] = "python-django",
        ["django"] = "python-django",
    };

    private readonly SkillActivationOptions _options;
    private readonly object _lock = new();
    private Dictionary<string, SkillManifestEntry>? _byId;

    public FileSkillManifestRegistry(IOptions<SkillActivationOptions> options) => _options = options.Value;

    public IReadOnlyList<SkillManifestEntry> List() =>
        GetIndex().Values.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase).ToList();

    public SkillManifestEntry? Find(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return null;

        var normalized = Normalize(skillName);
        if (Aliases.TryGetValue(normalized, out var alias))
            normalized = alias;

        return GetIndex().TryGetValue(normalized, out var entry) ? entry : null;
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

    public Task<string> LoadContentAsync(string skillName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var entry = Find(skillName);
        if (entry is null)
            return Task.FromResult(string.Empty);

        if (!File.Exists(entry.FilePath))
            return Task.FromResult(string.Empty);

        return File.ReadAllTextAsync(entry.FilePath, ct);
    }

    public void Refresh()
    {
        lock (_lock)
            _byId = null;
    }

    private Dictionary<string, SkillManifestEntry> GetIndex()
    {
        lock (_lock)
            return _byId ??= BuildIndex();
    }

    private Dictionary<string, SkillManifestEntry> BuildIndex()
    {
        var index = new Dictionary<string, SkillManifestEntry>(StringComparer.OrdinalIgnoreCase);
        IndexBundledSkills(index);
        IndexCrystallizedSkills(index);
        return index;
    }

    private void IndexBundledSkills(Dictionary<string, SkillManifestEntry> index)
    {
        var root = _options.SkillsRoot;
        if (!Directory.Exists(root))
            return;

        foreach (var skillDir in Directory.EnumerateDirectories(root))
        {
            var skillFile = Path.Combine(skillDir, "SKILL.md");
            if (!File.Exists(skillFile))
                continue;

            var folderId = Path.GetFileName(skillDir);
            RegisterSkillFile(index, skillFile, folderId);
        }
    }

    private void IndexCrystallizedSkills(Dictionary<string, SkillManifestEntry> index)
    {
        var root = ResolveCrystallizedRoot();
        if (!Directory.Exists(root))
            return;

        foreach (var skillFile in Directory.EnumerateFiles(root, "*.md", SearchOption.TopDirectoryOnly))
        {
            var content = File.ReadAllText(skillFile);
            if (IsPendingApproval(content))
                continue;

            var fileId = Path.GetFileNameWithoutExtension(skillFile);
            RegisterSkillFile(index, skillFile, $"crystallized-{fileId}");
        }
    }

    private void RegisterSkillFile(Dictionary<string, SkillManifestEntry> index, string skillFile, string fallbackId)
    {
        var content = File.ReadAllText(skillFile);
        var meta = SkillParser.Parse(content);
        var id = string.IsNullOrWhiteSpace(meta.Name) ? fallbackId : meta.Name;
        var description = string.IsNullOrWhiteSpace(meta.Description)
            ? fallbackId
            : meta.Description;

        var entry = new SkillManifestEntry(id, description, skillFile);
        index[id] = entry;
        if (!index.ContainsKey(fallbackId))
            index[fallbackId] = entry;
    }

    private string ResolveCrystallizedRoot()
    {
        if (Path.IsPathRooted(_options.CrystallizedSkillsRoot))
            return _options.CrystallizedSkillsRoot;

        return Path.GetFullPath(_options.CrystallizedSkillsRoot);
    }

    private static bool IsPendingApproval(string content) =>
        content.Contains("approval: pending", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string skillName) =>
        skillName.Trim().ToLowerInvariant().Replace(' ', '-');

    private static string TruncateOneLiner(string text, int max)
    {
        var oneLine = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return oneLine.Length <= max ? oneLine : oneLine[..max] + "…";
    }
}
