using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Libr4.IDE.Application.CodeSearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public sealed class HermesVectorSyncService : IHermesVectorSyncService
{
    private readonly SqliteHermesMemoryStore _memory;
    private readonly IVectorMemoryStore _vectorStore;
    private readonly IEmbeddingService _embeddings;
    private readonly QdrantSyncOptions _options;
    private readonly ILogger<HermesVectorSyncService> _logger;

    public HermesVectorSyncService(
        SqliteHermesMemoryStore memory,
        IVectorMemoryStore vectorStore,
        IEmbeddingService embeddings,
        IOptions<QdrantSyncOptions> options,
        ILogger<HermesVectorSyncService> logger)
    {
        _memory = memory;
        _vectorStore = vectorStore;
        _embeddings = embeddings;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SyncEntryAsync(HermesMemoryEntry entry, CancellationToken ct = default)
    {
        if (!_options.UseQdrantSync || string.IsNullOrWhiteSpace(entry.Summary))
            return;

        var text = $"{entry.Key} {entry.Summary}".Trim();
        var embedding = await _embeddings.EmbedAsync(text, ct).ConfigureAwait(false);
        var vectorId = BuildVectorId(entry);

        await _vectorStore.UpsertAsync(
            new VectorRecord(
                vectorId,
                _options.CollectionId,
                embedding,
                text,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["memory_id"] = entry.Id.ToString(),
                    ["run_id"] = entry.RunId.ToString(),
                    ["fingerprint"] = entry.RequestFingerprint,
                    ["kind"] = entry.Kind.ToString(),
                    ["key"] = entry.Key,
                    ["stage"] = entry.Stage
                }),
            ct).ConfigureAwait(false);

        _logger.LogDebug("Synced Hermes memory {Key} to vector index", entry.Key);
    }

    public Task RemoveEntryAsync(HermesMemoryEntry entry, CancellationToken ct = default)
    {
        if (!_options.UseQdrantSync)
            return Task.CompletedTask;

        var vectorId = BuildVectorId(entry);
        return _vectorStore.DeleteAsync(vectorId, _options.CollectionId, ct);
    }

    public async Task<int> BackfillAsync(CancellationToken ct = default)
    {
        if (!_options.UseQdrantSync)
            return 0;

        var entries = await _memory.ListAllAsync(ct).ConfigureAwait(false);
        var synced = 0;

        foreach (var batch in entries.Chunk(Math.Max(1, _options.BackfillBatchSize)))
        {
            foreach (var entry in batch)
            {
                await SyncEntryAsync(entry, ct).ConfigureAwait(false);
                synced++;
            }
        }

        _logger.LogInformation("Backfilled {Count} Hermes memories into vector index", synced);
        return synced;
    }

    internal static string BuildVectorId(HermesMemoryEntry entry) =>
        $"{entry.RequestFingerprint}|{entry.Key}";
}
