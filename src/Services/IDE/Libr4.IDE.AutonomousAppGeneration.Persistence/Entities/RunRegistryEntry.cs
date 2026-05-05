namespace Libr4.IDE.Application.AutonomousAppGeneration.Persistence.Entities;

/// <summary>
/// P2-1 of audit roadmap. Persistent metadata projection of an AppGenerationOrchestrator.
/// Stores enough to (a) survive a host restart for idempotency, (b) audit historical runs,
/// (c) support future migration to full state persistence.
///
/// Full domain state remains in <see cref="InMemoryAppGenerationRepository"/> until
/// <see cref="AppGenerationOrchestrator"/> grows a snapshot/rehydrate API.
/// </summary>
public sealed class RunRegistryEntry
{
    public Guid Id { get; set; }
    public string Fingerprint { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? FailureReason { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public int IterationCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Optional opaque JSON blob; reserved for future full snapshot.</summary>
    public string? PayloadJson { get; set; }
}
