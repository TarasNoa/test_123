using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.MetaAgent;

public sealed class FileAgentSpecVersionStore : IAgentSpecVersionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AgentSpecEvolutionOptions _options;

    public FileAgentSpecVersionStore(IOptions<AgentSpecEvolutionOptions> options) =>
        _options = options.Value;

    public Task<int> GetLatestVersionAsync(string specName, CancellationToken ct = default)
    {
        var dir = SpecVersionsDir(specName);
        if (!Directory.Exists(dir))
            return Task.FromResult(0);

        var max = Directory.EnumerateFiles(dir, "v*.agent.yaml")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name?.StartsWith('v') == true && int.TryParse(name[1..], out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();
        return Task.FromResult(max);
    }

    public async Task<string> SaveVersionAsync(
        string specName,
        int version,
        AgentSpecDocument document,
        string changeSummary,
        Guid? sourceProposalId,
        CancellationToken ct = default)
    {
        var dir = SpecVersionsDir(specName);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"v{version}.agent.yaml");
        await File.WriteAllTextAsync(path, AgentSpecYamlWriter.Write(document), ct).ConfigureAwait(false);

        var metaPath = Path.Combine(dir, "changelog.jsonl");
        var entry = new AgentSpecChangelogEntry(
            specName,
            version,
            changeSummary,
            null,
            sourceProposalId,
            DateTime.UtcNow);
        await File.AppendAllTextAsync(metaPath, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, ct)
            .ConfigureAwait(false);

        return path;
    }

    public Task<IReadOnlyList<AgentSpecVersionRecord>> ListVersionsAsync(string specName, CancellationToken ct = default)
    {
        var dir = SpecVersionsDir(specName);
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<AgentSpecVersionRecord>>(Array.Empty<AgentSpecVersionRecord>());

        var records = Directory.EnumerateFiles(dir, "v*.agent.yaml")
            .Select(path =>
            {
                var file = Path.GetFileNameWithoutExtension(path);
                var version = file.StartsWith('v') && int.TryParse(file[1..], out var v) ? v : 0;
                return new AgentSpecVersionRecord(
                    specName,
                    version,
                    path,
                    $"version {version}",
                    null,
                    File.GetCreationTimeUtc(path));
            })
            .OrderBy(r => r.Version)
            .ToList();

        return Task.FromResult<IReadOnlyList<AgentSpecVersionRecord>>(records);
    }

    public async Task<IReadOnlyList<AgentSpecChangelogEntry>> GetChangelogAsync(string specName, CancellationToken ct = default)
    {
        var metaPath = Path.Combine(SpecVersionsDir(specName), "changelog.jsonl");
        if (!File.Exists(metaPath))
            return Array.Empty<AgentSpecChangelogEntry>();

        var entries = new List<AgentSpecChangelogEntry>();
        foreach (var line in await File.ReadAllLinesAsync(metaPath, ct).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var entry = JsonSerializer.Deserialize<AgentSpecChangelogEntry>(line, JsonOptions);
            if (entry is not null)
                entries.Add(entry);
        }

        return entries;
    }

    private string SpecVersionsDir(string specName)
    {
        var root = Path.IsPathRooted(_options.VersionsRoot)
            ? _options.VersionsRoot
            : Path.GetFullPath(_options.VersionsRoot);
        return Path.Combine(root, specName);
    }
}
