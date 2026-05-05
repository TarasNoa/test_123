using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;

#if INTERNAL
/// <summary>
/// Implementation of autonomous memory consolidation service.
/// Performs background summarization, deduplication, and clustering of memory records.
/// INTERNAL: This service is for internal use only and will not be included in public builds.
/// </summary>
public class AutonomousMemoryConsolidationService : IAutonomousMemoryConsolidationService
{
    private readonly ILogger<AutonomousMemoryConsolidationService> _logger;
    private readonly IMemoryStore _memoryStore;
    private readonly Dictionary<Guid, ConsolidationStatus> _consolidationStatuses = new();
    private readonly List<ConsolidationTelemetry> _telemetryHistory = new();

    public AutonomousMemoryConsolidationService(
        ILogger<AutonomousMemoryConsolidationService> logger,
        IMemoryStore memoryStore)
    {
        _logger = logger;
        _memoryStore = memoryStore;
    }

    public async Task TriggerConsolidationAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        _logger.LogInformation("[Consolidation] Starting consolidation for run {RunId}", runId);
        
        try
        {
            var status = new ConsolidationStatus
            {
                RunId = runId,
                Status = "running",
                StartedAt = startedAt
            };
            _consolidationStatuses[runId] = status;

            var allRecords = new List<MemoryRecord>();
            await foreach (var record in _memoryStore.RetrieveAsync(new MemoryQuery(), cancellationToken))
            {
                allRecords.Add(record);
            }

            var recordsProcessed = allRecords.Count;
            _logger.LogInformation("[Consolidation] Retrieved {Count} records for consolidation", recordsProcessed);

            var deduplicated = DeduplicateRecords(allRecords);
            var recordsAfterDeduplication = deduplicated.Count;
            _logger.LogInformation("[Consolidation] Deduplicated to {Count} records", recordsAfterDeduplication);

            var clustered = ClusterRecords(deduplicated);
            var clustersCreated = clustered.Count;
            _logger.LogInformation("[Consolidation] Created {Count} clusters", clustersCreated);

            var summarized = await SummarizeRecords(clustered, cancellationToken);
            var recordsSummarized = summarized.Count;
            _logger.LogInformation("[Consolidation] Summarized {Count} records", recordsSummarized);

            await IngestConsolidatedRecordsAsync(summarized, cancellationToken);
            var recordsConsolidated = summarized.Count;
            _logger.LogInformation("[Consolidation] Ingested {Count} consolidated records", recordsConsolidated);

            var patternsConsolidated = ExtractPatterns(clustered);
            _logger.LogInformation("[Consolidation] Consolidated patterns: {Patterns}", string.Join(", ", patternsConsolidated));

            status.Status = "completed";
            status.CompletedAt = DateTime.UtcNow;
            status.RecordsProcessed = recordsProcessed;
            status.RecordsConsolidated = recordsConsolidated;

            // Record telemetry
            var telemetry = new ConsolidationTelemetry
            {
                RunId = runId,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                RecordsProcessed = recordsProcessed,
                RecordsAfterDeduplication = recordsAfterDeduplication,
                ClustersCreated = clustersCreated,
                RecordsSummarized = recordsSummarized,
                RecordsConsolidated = recordsConsolidated,
                Success = true,
                PatternsConsolidated = patternsConsolidated
            };
            _telemetryHistory.Add(telemetry);

            // Check for effectiveness alert
            CheckConsolidationEffectiveness();

            _logger.LogInformation("[Consolidation] Completed for run {RunId}", runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Consolidation] Failed for run {RunId}", runId);
            
            if (_consolidationStatuses.TryGetValue(runId, out var status))
            {
                status.Status = "failed";
                status.CompletedAt = DateTime.UtcNow;
                status.ErrorMessage = ex.Message;
            }

            // Record failure telemetry
            var telemetry = new ConsolidationTelemetry
            {
                RunId = runId,
                StartedAt = startedAt,
                CompletedAt = DateTime.UtcNow,
                RecordsProcessed = 0,
                RecordsAfterDeduplication = 0,
                ClustersCreated = 0,
                RecordsSummarized = 0,
                RecordsConsolidated = 0,
                Success = false,
                ErrorMessage = ex.Message
            };
            _telemetryHistory.Add(telemetry);
        }
    }

    public async Task TriggerGlobalConsolidationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting global memory consolidation");
        
        // This would typically iterate over all runs that haven't been consolidated recently
        // For now, this is a placeholder implementation
        _logger.LogWarning("Global consolidation not fully implemented yet");
        
        await Task.CompletedTask;
    }

    public Task<ConsolidationStatus?> GetConsolidationStatusAsync(Guid runId)
    {
        ConsolidationStatus? status = _consolidationStatuses.TryGetValue(runId, out var s) ? s : null;
        return Task.FromResult(status);
    }

    public Task<ConsolidationStatistics> GetStatisticsAsync()
    {
        // Placeholder statistics
        return Task.FromResult(new ConsolidationStatistics(
            TotalRecords: 0,
            ConsolidatedRecords: 0,
            DuplicateRecordsRemoved: 0,
            SummariesGenerated: 0,
            CompressionRatio: 0.0,
            LastConsolidatedAtUtc: DateTime.UtcNow));
    }

    /// <summary>
    /// Retrieves all memory records for a specific run.
    /// </summary>
    private async Task<List<MemoryRecord>> RetrieveAllRecordsAsync(Guid runId, CancellationToken cancellationToken)
    {
        // Since IMemoryStore doesn't have a GetAllRecordsAsync method,
        // we'll use a query to retrieve records for the run
        var query = new MemoryQuery(
            RequestFingerprint: runId.ToString(),
            Keyword: null,
            TopK: 1000);
        var results = await _memoryStore.RetrieveAsync(query, cancellationToken);
        return results.Select(r => r.Record).ToList();
    }

    /// <summary>
    /// Removes duplicate records based on content similarity.
    /// </summary>
    private List<MemoryRecord> DeduplicateRecords(List<MemoryRecord> records)
    {
        var uniqueRecords = new List<MemoryRecord>();
        var seenKeys = new HashSet<string>();

        foreach (var record in records)
        {
            var key = $"{record.Kind}:{record.Key}:{record.Stage}:{record.Summary.GetHashCode()}";
            if (!seenKeys.Contains(key))
            {
                seenKeys.Add(key);
                uniqueRecords.Add(record);
            }
        }

        return uniqueRecords;
    }

    /// <summary>
    /// Clusters records by kind and key for summarization.
    /// </summary>
    private Dictionary<string, List<MemoryRecord>> ClusterRecords(List<MemoryRecord> records)
    {
        var clusters = new Dictionary<string, List<MemoryRecord>>();

        foreach (var record in records)
        {
            var clusterKey = $"{record.Kind}:{record.Key}:{record.Stage}";
            if (!clusters.ContainsKey(clusterKey))
            {
                clusters[clusterKey] = new List<MemoryRecord>();
            }
            clusters[clusterKey].Add(record);
        }

        return clusters;
    }

    /// <summary>
    /// Summarizes clusters with more than a threshold number of records.
    /// </summary>
    private async Task<List<MemoryRecord>> SummarizeClustersAsync(
        Dictionary<string, List<MemoryRecord>> clusters,
        CancellationToken cancellationToken)
    {
        var result = new List<MemoryRecord>();
        const int SummaryThreshold = 5;

        foreach (var (clusterKey, records) in clusters)
        {
            if (records.Count < SummaryThreshold)
            {
                result.AddRange(records);
                continue;
            }

            // Create a summary record for the cluster
            var summaryRecord = CreateSummaryRecord(clusterKey, records);
            result.Add(summaryRecord);

            // Keep the most recent records
            var recentRecords = records
                .OrderByDescending(r => r.CreatedAtUtc)
                .Take(2)
                .ToList();
            result.AddRange(recentRecords);
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Creates a summary record from a cluster of records.
    /// </summary>
    private MemoryRecord CreateSummaryRecord(string clusterKey, List<MemoryRecord> records)
    {
        var firstRecord = records.First();
        var kind = firstRecord.Kind;
        var key = firstRecord.Key;
        var stage = firstRecord.Stage;

        var summary = $"Consolidated {records.Count} records. " +
                     $"Earliest: {records.Min(r => r.CreatedAtUtc):O}, " +
                     $"Latest: {records.Max(r => r.CreatedAtUtc):O}. " +
                     $"Total tokens: {records.Sum(r => r.TokenEstimate)}.";

        return new MemoryRecord(
            RunId: firstRecord.RunId,
            RequestFingerprint: firstRecord.RequestFingerprint,
            Stage: stage,
            Kind: kind,
            Key: $"{key}_summary",
            Summary: summary,
            PayloadJson: null,
            TokenEstimate: summary.Length / 4, // Rough estimate
            CreatedAtUtc: DateTime.UtcNow);
    }

    /// <summary>
    /// Updates the memory store with consolidated records.
    /// </summary>
    private async Task UpdateMemoryStoreAsync(
        Guid runId,
        List<MemoryRecord> consolidatedRecords,
        CancellationToken cancellationToken)
    {
        // Ingest consolidated records into memory store
        foreach (var record in consolidatedRecords)
        {
            await _memoryStore.IngestAsync(record, cancellationToken);
        }
    }

    private static IReadOnlyList<string> ExtractPatterns(List<List<MemoryRecord>> clusters)
    {
        var patterns = new List<string>();
        foreach (var cluster in clusters)
        {
            if (cluster.Count == 0) continue;
            
            // Extract pattern from cluster key or summary
            var firstRecord = cluster[0];
            if (!string.IsNullOrEmpty(firstRecord.Summary))
            {
                patterns.Add(firstRecord.Summary);
            }
            else if (!string.IsNullOrEmpty(firstRecord.Key))
            {
                patterns.Add(firstRecord.Key);
            }
        }
        return patterns;
    }

    private void CheckConsolidationEffectiveness()
    {
        const int recentRuns = 5;
        var recentTelemetry = _telemetryHistory.TakeLast(recentRuns).ToList();
        
        if (recentTelemetry.Count < 3)
        {
            _logger.LogDebug("[Consolidation] Not enough data to check effectiveness (need at least 3 runs, have {Count})", recentTelemetry.Count);
            return;
        }

        var avgEffectiveness = recentTelemetry.Average(t => t.EffectivenessScore);
        var avgConsolidationRatio = recentTelemetry.Average(t => t.ConsolidationRatio);

        _logger.LogInformation(
            "[Consolidation] Effectiveness check: AvgEffectiveness={Effectiveness:P0}, AvgConsolidationRatio={Ratio:P0}",
            avgEffectiveness, avgConsolidationRatio);

        if (avgEffectiveness < 0.3)
        {
            _logger.LogWarning(
                "[Consolidation] ALERT: Consolidation effectiveness is low ({Effectiveness:P0}). Consider reviewing consolidation strategy.",
                avgEffectiveness);
        }

        if (avgConsolidationRatio > 0.9)
        {
            _logger.LogWarning(
                "[Consolidation] ALERT: Consolidation is not reducing records (ratio={Ratio:P0}). Consider adjusting clustering/deduplication thresholds.",
                avgConsolidationRatio);
        }
    }

    /// <summary>
    /// Gets the consolidation telemetry history.
    /// </summary>
    public IReadOnlyList<ConsolidationTelemetry> GetTelemetryHistory()
    {
        return _telemetryHistory.AsReadOnly();
    }

    /// <summary>
    /// Gets the average effectiveness score over the last N consolidations.
    /// </summary>
    public double GetAverageEffectiveness(int lastN = 5)
    {
        var recentTelemetry = _telemetryHistory.TakeLast(lastN).ToList();
        if (recentTelemetry.Count == 0) return 0;
        return recentTelemetry.Average(t => t.EffectivenessScore);
    }

    /// <summary>
    /// Compares quality metrics before and after consolidation for A/B testing.
    /// </summary>
    public ConsolidationComparison? CompareRuns(Guid beforeRunId, Guid afterRunId)
    {
        // This would typically retrieve quality metrics from the orchestrator or quality gate service
        // For now, this is a placeholder that would be integrated with the run tracking system
        _logger.LogInformation(
            "[Consolidation] Comparing runs: Before={BeforeRunId}, After={AfterRunId}",
            beforeRunId, afterRunId);
        
        // Placeholder: In a real implementation, this would query the orchestrator
        // for quality scores, error counts, and fix iterations for both runs
        return null;
    }
}
#endif
