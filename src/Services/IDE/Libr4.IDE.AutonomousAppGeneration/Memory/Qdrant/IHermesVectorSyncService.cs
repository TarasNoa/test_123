using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public interface IHermesVectorSyncService
{
    Task SyncEntryAsync(HermesMemoryEntry entry, CancellationToken ct = default);

    Task RemoveEntryAsync(HermesMemoryEntry entry, CancellationToken ct = default);

    Task<int> BackfillAsync(CancellationToken ct = default);
}
