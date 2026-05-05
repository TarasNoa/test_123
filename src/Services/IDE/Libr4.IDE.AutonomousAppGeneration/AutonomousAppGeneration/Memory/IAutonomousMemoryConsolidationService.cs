namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;

/// <summary>
/// Service for background consolidation of autonomous generation memory.
/// Performs summarization, deduplication, and clustering of memory records
/// to optimize storage and improve retrieval relevance.
/// </summary>
public interface IAutonomousMemoryConsolidationService
{
    /// <summary>
    /// Triggers a background consolidation process for a specific run.
    /// </summary>
    /// <param name="runId">The run ID to consolidate memory for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the consolidation operation.</returns>
    Task TriggerConsolidationAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers consolidation for all runs that haven't been consolidated recently.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the consolidation operation.</returns>
    Task TriggerGlobalConsolidationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the consolidation status for a specific run.
    /// </summary>
    /// <param name="runId">The run ID to check.</param>
    /// <returns>Consolidation status or null if not found.</returns>
    Task<ConsolidationStatus?> GetConsolidationStatusAsync(Guid runId);

    /// <summary>
    /// Gets statistics about consolidated memory.
    /// </summary>
    /// <returns>Consolidation statistics.</returns>
    Task<ConsolidationStatistics> GetStatisticsAsync();
}

/// <summary>
/// Status of memory consolidation for a run.
/// </summary>
public enum ConsolidationStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Statistics about consolidated memory.
/// </summary>
public sealed record ConsolidationStatistics(
    int TotalRecords,
    int ConsolidatedRecords,
    int DuplicateRecordsRemoved,
    int SummariesGenerated,
    double CompressionRatio,
    DateTime LastConsolidatedAtUtc);
