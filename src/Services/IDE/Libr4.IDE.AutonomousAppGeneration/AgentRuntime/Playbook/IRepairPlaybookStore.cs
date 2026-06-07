namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

public interface IRepairPlaybookStore
{
    Task EnsureSchemaAsync(CancellationToken ct = default);

    Task<string?> TryGetHintAsync(string errorSignature, CancellationToken ct = default);

    Task RecordOutcomeAsync(
        string errorSignature,
        string fixPattern,
        bool succeeded,
        string? stackPattern = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<RepairPlaybookEntry>> ListTopAsync(int limit = 20, CancellationToken ct = default);

    Task<RepairPlaybookEntry?> GetBySignatureAsync(string errorSignature, CancellationToken ct = default);
}
