namespace Libr4.IDE.Domain.AutonomousAppGeneration;

public sealed record TriggerIngestionAuditEntry(
    Guid RunId,
    string Source,
    string AdapterName,
    string NormalizedRequest,
    string? Actor,
    string? CorrelationId,
    DateTime ReceivedAtUtc);
