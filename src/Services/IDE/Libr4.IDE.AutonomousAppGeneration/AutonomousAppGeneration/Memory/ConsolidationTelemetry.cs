namespace Libr4.IDE.AutonomousAppGeneration.AutonomousAppGeneration.Memory;

/// <summary>
/// Telemetry data for memory consolidation operations.
/// Used to measure effectiveness and trigger alerts if consolidation is not providing value.
/// </summary>
public class ConsolidationTelemetry
{
    /// <summary>
    /// Run ID that triggered consolidation.
    /// </summary>
    public Guid RunId { get; init; }

    /// <summary>
    /// Timestamp when consolidation started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// Timestamp when consolidation completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Duration of consolidation operation.
    /// </summary>
    public TimeSpan Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Number of records processed.
    /// </summary>
    public int RecordsProcessed { get; init; }

    /// <summary>
    /// Number of records after deduplication.
    /// </summary>
    public int RecordsAfterDeduplication { get; init; }

    /// <summary>
    /// Number of clusters created.
    /// </summary>
    public int ClustersCreated { get; init; }

    /// <summary>
    /// Number of records summarized.
    /// </summary>
    public int RecordsSummarized { get; init; }

    /// <summary>
    /// Number of consolidated records ingested back.
    /// </summary>
    public int RecordsConsolidated { get; init; }

    /// <summary>
    /// Consolidation ratio (records after / records before).
    /// </summary>
    public double ConsolidationRatio => RecordsProcessed > 0 
        ? (double)RecordsConsolidated / RecordsProcessed 
        : 0;

    /// <summary>
    /// Whether consolidation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if consolidation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Patterns that were consolidated (e.g., "http_error_handling", "database_connection").
    /// </summary>
    public IReadOnlyList<string> PatternsConsolidated { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Effectiveness score (0-1) based on consolidation ratio and pattern quality.
    /// </summary>
    public double EffectivenessScore => CalculateEffectivenessScore();

    private double CalculateEffectivenessScore()
    {
        if (!Success || RecordsProcessed == 0)
            return 0;

        // Base score from consolidation ratio (target: reduce to 30-50% of original)
        var ratioScore = ConsolidationRatio <= 0.5 ? 1.0 : 
                        ConsolidationRatio <= 0.7 ? 0.8 : 
                        ConsolidationRatio <= 0.9 ? 0.5 : 0.2;

        // Bonus for pattern diversity
        var patternBonus = PatternsConsolidated.Count > 0 ? 0.2 : 0;

        // Penalty if no actual consolidation happened
        var noConsolidationPenalty = RecordsConsolidated >= RecordsProcessed ? 0.3 : 0;

        return Math.Clamp(ratioScore + patternBonus - noConsolidationPenalty, 0, 1);
    }
}

/// <summary>
/// Comparison of quality before and after consolidation for A/B testing.
/// </summary>
public class ConsolidationComparison
{
    /// <summary>
    /// Run ID before consolidation.
    /// </summary>
    public Guid BeforeRunId { get; init; }

    /// <summary>
    /// Run ID after consolidation.
    /// </summary>
    public Guid AfterRunId { get; init; }

    /// <summary>
    /// Quality score before consolidation.
    /// </summary>
    public double BeforeQualityScore { get; init; }

    /// <summary>
    /// Quality score after consolidation.
    /// </summary>
    public double AfterQualityScore { get; init; }

    /// <summary>
    /// Number of errors before consolidation.
    /// </summary>
    public int BeforeErrorCount { get; init; }

    /// <summary>
    /// Number of errors after consolidation.
    /// </summary>
    public int AfterErrorCount { get; init; }

    /// <summary>
    /// Number of fix iterations before consolidation.
    /// </summary>
    public int BeforeFixIterations { get; init; }

    /// <summary>
    /// Number of fix iterations after consolidation.
    /// </summary>
    public int AfterFixIterations { get; init; }

    /// <summary>
    /// Whether consolidation improved quality.
    /// </summary>
    public bool Improved => AfterQualityScore > BeforeQualityScore;

    /// <summary>
    /// Quality improvement percentage.
    /// </summary>
    public double ImprovementPercentage => BeforeQualityScore > 0 
        ? ((AfterQualityScore - BeforeQualityScore) / BeforeQualityScore) * 100 
        : 0;
}
