using System.Collections.Concurrent;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

public sealed record ProviderCostEntry(
    Guid RunId,
    string ProviderId,
    string Stage,
    string? ModelId,
    long Tokens,
    decimal CostUsd,
    DateTime RecordedAtUtc);

public sealed record ProviderCostSummary(
    string ProviderId,
    long TotalTokens,
    decimal TotalCostUsd,
    int RequestCount);

public interface IRunProviderCostTracker
{
    Task RecordAsync(
        Guid runId,
        string providerId,
        string stage,
        string? modelId,
        long tokens,
        decimal costUsd,
        CancellationToken ct = default);

    IReadOnlyList<ProviderCostEntry> GetEntries(Guid runId);

    IReadOnlyDictionary<string, ProviderCostSummary> RollupByProvider(Guid runId);
}

public sealed class RunProviderCostTracker : IRunProviderCostTracker
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<Guid, List<ProviderCostEntry>> _memory = new();
    private readonly AgentRuntimeOptions _options;

    public RunProviderCostTracker(IOptions<AgentRuntimeOptions> options) =>
        _options = options.Value;

    public async Task RecordAsync(
        Guid runId,
        string providerId,
        string stage,
        string? modelId,
        long tokens,
        decimal costUsd,
        CancellationToken ct = default)
    {
        var entry = new ProviderCostEntry(
            runId,
            providerId,
            stage,
            modelId,
            Math.Max(0, tokens),
            Math.Max(0, costUsd),
            DateTime.UtcNow);

        _memory.AddOrUpdate(
            runId,
            _ => [entry],
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(entry);
                    return list;
                }
            });

        var path = CostFilePath(runId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.AppendAllTextAsync(path, JsonSerializer.Serialize(entry, JsonOptions) + Environment.NewLine, ct)
            .ConfigureAwait(false);
    }

    public IReadOnlyList<ProviderCostEntry> GetEntries(Guid runId)
    {
        if (_memory.TryGetValue(runId, out var cached))
        {
            lock (cached)
                return cached.ToList();
        }

        var path = CostFilePath(runId);
        if (!File.Exists(path))
            return Array.Empty<ProviderCostEntry>();

        var entries = new List<ProviderCostEntry>();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var entry = JsonSerializer.Deserialize<ProviderCostEntry>(line, JsonOptions);
            if (entry is not null)
                entries.Add(entry);
        }

        _memory[runId] = entries;
        return entries;
    }

    public IReadOnlyDictionary<string, ProviderCostSummary> RollupByProvider(Guid runId) =>
        GetEntries(runId)
            .GroupBy(e => e.ProviderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => new ProviderCostSummary(
                    g.Key,
                    g.Sum(x => x.Tokens),
                    g.Sum(x => x.CostUsd),
                    g.Count()),
                StringComparer.OrdinalIgnoreCase);

    private string CostFilePath(Guid runId) =>
        Path.Combine(_options.RunsRoot, runId.ToString("D"), "provider-cost.jsonl");
}
