using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Domain.AgentMemorySystem;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;

public interface ICognitiveMemoryBridge
{
    CognitiveMemorySystem GetOrCreateSystem(string fingerprint, string? agentId = null);

    Task SyncFromHermesEntryAsync(HermesMemoryEntry entry, CancellationToken ct = default);

    Task<IReadOnlyList<LayeredMemoryFragment>> SearchLayerAsync(
        string fingerprint,
        MemoryLayer layer,
        string query,
        int topN = 10,
        CancellationToken ct = default);

    MemorySystemStatistics GetStatistics(string fingerprint);

    Task<int> BackfillFromHermesAsync(CancellationToken ct = default);
}
