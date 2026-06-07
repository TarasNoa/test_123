using System.Collections.Concurrent;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;

[Obsolete("Legacy in-memory store for unit tests only. Production uses SqliteHermesMemoryStore via IMemoryStore.")]
public sealed class InMemoryMemoryStore : IMemoryStore
{
    private readonly ConcurrentDictionary<string, List<MemoryRecord>> _recordsByFingerprint = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public Task IngestAsync(MemoryRecord record, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_lock)
        {
            var records = _recordsByFingerprint.GetOrAdd(record.RequestFingerprint, _ => new List<MemoryRecord>());
            var existingIndex = records.FindIndex(r => string.Equals(r.Key, record.Key, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                records[existingIndex] = record;
            }
            else
            {
                records.Add(record);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MemoryRetrievalResult>> RetrieveAsync(MemoryQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!_recordsByFingerprint.TryGetValue(query.RequestFingerprint, out var records))
            return Task.FromResult<IReadOnlyList<MemoryRetrievalResult>>(Array.Empty<MemoryRetrievalResult>());

        var filtered = records
            .Where(record => query.Kinds is null || query.Kinds.Length == 0 || query.Kinds.Contains(record.Kind))
            .Where(record => string.IsNullOrWhiteSpace(query.Keyword)
                || record.Summary.Contains(query.Keyword!, StringComparison.OrdinalIgnoreCase)
                || record.Key.Contains(query.Keyword!, StringComparison.OrdinalIgnoreCase)
                || record.Stage.Contains(query.Keyword!, StringComparison.OrdinalIgnoreCase))
            .Select(record => new MemoryRetrievalResult(
                record,
                BuildRetrievalReason(record, query.Keyword),
                ComputeRelevanceScore(record, query.Keyword)))
            .OrderByDescending(result => result.RelevanceScore)
            .ThenByDescending(result => result.Record.CreatedAtUtc)
            .Take(Math.Max(0, query.TopK))
            .ToList();

        return Task.FromResult<IReadOnlyList<MemoryRetrievalResult>>(filtered);
    }

    public Task PruneAsync(string requestFingerprint, int maxTokenBudget, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(requestFingerprint))
            return Task.CompletedTask;

        if (maxTokenBudget <= 0)
        {
            _recordsByFingerprint.TryRemove(requestFingerprint, out _);
            return Task.CompletedTask;
        }

        if (!_recordsByFingerprint.TryGetValue(requestFingerprint, out var records))
            return Task.CompletedTask;

        lock (_lock)
        {
            if (!_recordsByFingerprint.TryGetValue(requestFingerprint, out records))
                return Task.CompletedTask;

            var prioritized = records
                .OrderByDescending(record => ComputeRelevanceScore(record, keyword: null))
                .ThenByDescending(record => record.CreatedAtUtc)
                .ToList();

            var kept = new List<MemoryRecord>();
            var budgetUsed = 0;
            foreach (var record in prioritized)
            {
                if (budgetUsed + record.TokenEstimate > maxTokenBudget)
                    continue;

                kept.Add(record);
                budgetUsed += record.TokenEstimate;
            }

            _recordsByFingerprint[requestFingerprint] = kept;
        }

        return Task.CompletedTask;
    }

    private static double ComputeRelevanceScore(MemoryRecord record, string? keyword)
    {
        var score = record.Kind switch
        {
            MemoryKind.Procedural => 3.0,
            MemoryKind.Semantic => 2.0,
            _ => 1.0,
        };

        var ageHours = Math.Max(0.0, (DateTime.UtcNow - record.CreatedAtUtc).TotalHours);
        score += Math.Max(0.0, 1.0 - Math.Min(ageHours / 24.0, 1.0));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            if (record.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                score += 2.0;
            if (record.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                score += 1.5;
            if (record.Stage.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                score += 1.0;
        }

        return score;
    }

    private static string BuildRetrievalReason(MemoryRecord record, string? keyword)
    {
        var reason = record.Kind switch
        {
            MemoryKind.Procedural => "procedural_priority",
            MemoryKind.Semantic => "semantic_priority",
            _ => "episodic_priority",
        };

        if (!string.IsNullOrWhiteSpace(keyword) &&
            (record.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
             record.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
             record.Stage.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return $"{reason};keyword_match";
        }

        return reason;
    }
}
