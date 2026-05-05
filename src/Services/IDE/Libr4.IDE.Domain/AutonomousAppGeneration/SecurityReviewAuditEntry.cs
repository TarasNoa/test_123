namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record SecurityReviewAuditEntry(
    string Stage,
    int Score,
    bool Passed,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> RemediationHints,
    DateTime EvaluatedAtUtc);
