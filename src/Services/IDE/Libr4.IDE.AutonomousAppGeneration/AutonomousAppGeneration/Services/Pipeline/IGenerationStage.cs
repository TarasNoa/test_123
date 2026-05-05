using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

/// <summary>
/// P1-3 of audit roadmap. Strangler-fig scaffold for decomposing the ~730-line
/// <c>StartAppGenerationCommandHandler.Handle</c> into focused, testable stages.
///
/// New stages should:
///   1) declare a stable <see cref="Name"/> matching audit logs;
///   2) read/write only via <see cref="GenerationContext"/> — no static state;
///   3) return <see cref="StageOutcome.Continue"/> on success or
///      <see cref="StageOutcome.Stop"/> with <c>FailureReason</c> on a hard failure;
///   4) be unit-testable in isolation.
///
/// The orchestrator will call stages in registered order. This file ships the
/// abstractions and minimal stages; full migration of Handle is tracked separately.
/// </summary>
public interface IGenerationStage
{
    string Name { get; }

    /// <summary>Stages with smaller Order run first; ties broken by <see cref="Name"/>.</summary>
    int Order { get; }

    Task<StageOutcome> ExecuteAsync(GenerationContext context, CancellationToken ct);
}

public sealed class GenerationContext
{
    public required AppGenerationOrchestrator Orchestrator { get; init; }
    public required string UserRequest { get; init; }

    /// <summary>Idempotency lookup key (BLAKE-style fingerprint of request + max iterations).</summary>
    public string? Fingerprint { get; set; }

    /// <summary>Caller-supplied iteration budget (may be tighter than plan default).</summary>
    public int RequestedMaxIterations { get; set; }

    /// <summary>Set by short-circuiting stages (e.g. <c>IdempotencyCheckStage</c>) to bypass the rest.</summary>
    public AppGenerationOrchestrator? ShortCircuitOrchestrator { get; set; }

    public GenerationPlan? Plan { get; set; }
    public List<GeneratedFile> Files { get; } = new();
    public IReadOnlyList<GenerationPhaseBatchResult>? PhaseBatches { get; set; }
    public Guid? WorkspaceId { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Free-form bag for stage-to-stage payload; avoid for cross-cutting state.</summary>
    public Dictionary<string, object?> Items { get; } = new(StringComparer.Ordinal);
}

public sealed record StageOutcome(bool ShouldContinue, string? FailureReason = null, bool ShortCircuit = false)
{
    public static readonly StageOutcome Continue = new(true);
    public static StageOutcome Stop(string reason) => new(false, reason);

    /// <summary>Indicates a non-error early exit (e.g. idempotency reuse). Pipeline halts but is treated as success.</summary>
    public static readonly StageOutcome ShortCircuitSuccess = new(false, null, ShortCircuit: true);
}
