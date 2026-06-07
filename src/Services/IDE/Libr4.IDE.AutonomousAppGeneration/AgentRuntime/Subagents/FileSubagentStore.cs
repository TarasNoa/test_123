using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;

public sealed class FileSubagentStore : ISubagentStore
{
    private readonly AgentRuntimeOptions _options;
    private readonly ISerializer _yaml;

    public FileSubagentStore(IOptions<AgentRuntimeOptions> options)
    {
        _options = options.Value;
        _yaml = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public async Task<SubagentRecord> CreateAsync(Guid runId, string name, string task, AgentSpec? spec, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        var dir = SubagentDir(runId, id);
        Directory.CreateDirectory(dir);

        if (spec is not null)
        {
            var specPath = Path.Combine(dir, "spec.yaml");
            await File.WriteAllTextAsync(specPath, _yaml.Serialize(new AgentSpecDocument
            {
                Name = spec.Name,
                Model = spec.Model,
                MaxTurns = spec.MaxTurns,
                MaxTokens = spec.MaxTokens,
                Toolset = spec.Toolset.ToList(),
                Instruction = spec.Instruction,
                Permissions = spec.Permissions
            }), ct).ConfigureAwait(false);
        }

        var now = DateTime.UtcNow;
        var record = new SubagentRecord(id, runId, name, task, "running", now, now, null, null);
        await WriteStatusAsync(dir, record, ct).ConfigureAwait(false);
        return record;
    }

    public async Task AppendMessageAsync(Guid runId, string subagentId, string role, string content, CancellationToken ct = default)
    {
        var path = Path.Combine(SubagentDir(runId, subagentId), "messages.jsonl");
        var line = JsonSerializer.Serialize(new { role, content, at = DateTime.UtcNow });
        await File.AppendAllTextAsync(path, line + Environment.NewLine, ct).ConfigureAwait(false);
    }

    public async Task CompleteAsync(Guid runId, string subagentId, string output, CancellationToken ct = default)
    {
        var dir = SubagentDir(runId, subagentId);
        await File.WriteAllTextAsync(Path.Combine(dir, "output.md"), output, ct).ConfigureAwait(false);
        var existing = await GetAsync(runId, subagentId, ct).ConfigureAwait(false)
                       ?? new SubagentRecord(subagentId, runId, "unknown", string.Empty, "running", DateTime.UtcNow, DateTime.UtcNow, null, null);
        var updated = existing with
        {
            Status = "completed",
            UpdatedAtUtc = DateTime.UtcNow,
            OutputPreview = Preview(output)
        };
        await WriteStatusAsync(dir, updated, ct).ConfigureAwait(false);
    }

    public async Task FailAsync(Guid runId, string subagentId, string error, CancellationToken ct = default)
    {
        var dir = SubagentDir(runId, subagentId);
        var existing = await GetAsync(runId, subagentId, ct).ConfigureAwait(false)
                       ?? new SubagentRecord(subagentId, runId, "unknown", string.Empty, "running", DateTime.UtcNow, DateTime.UtcNow, null, null);
        var updated = existing with
        {
            Status = "failed",
            UpdatedAtUtc = DateTime.UtcNow,
            Error = error,
            OutputPreview = Preview(error)
        };
        await WriteStatusAsync(dir, updated, ct).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<SubagentRecord>> ListAsync(Guid runId, CancellationToken ct = default)
    {
        var root = RunSubagentsRoot(runId);
        if (!Directory.Exists(root))
            return Task.FromResult<IReadOnlyList<SubagentRecord>>(Array.Empty<SubagentRecord>());

        var records = new List<SubagentRecord>();
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var statusPath = Path.Combine(dir, "status.json");
            if (!File.Exists(statusPath))
                continue;
            var json = File.ReadAllText(statusPath);
            var record = JsonSerializer.Deserialize<SubagentRecord>(json);
            if (record is not null)
                records.Add(record);
        }

        records.Sort((a, b) => b.CreatedAtUtc.CompareTo(a.CreatedAtUtc));
        return Task.FromResult<IReadOnlyList<SubagentRecord>>(records);
    }

    public async Task<SubagentRecord?> GetAsync(Guid runId, string subagentId, CancellationToken ct = default)
    {
        var statusPath = Path.Combine(SubagentDir(runId, subagentId), "status.json");
        if (!File.Exists(statusPath))
            return null;
        var json = await File.ReadAllTextAsync(statusPath, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<SubagentRecord>(json);
    }

    private string SubagentDir(Guid runId, string subagentId) =>
        Path.Combine(RunSubagentsRoot(runId), subagentId);

    private string RunSubagentsRoot(Guid runId) =>
        Path.Combine(_options.RunsRoot, runId.ToString("D"), "subagents");

    private static async Task WriteStatusAsync(string dir, SubagentRecord record, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(record);
        await File.WriteAllTextAsync(Path.Combine(dir, "status.json"), json, ct).ConfigureAwait(false);
    }

    private static string? Preview(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Length <= 240 ? text : text[..240] + "...";
}
