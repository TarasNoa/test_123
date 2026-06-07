using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetRetentionService
{
    Task<FleetRetentionSweepResult> ApplyRetentionAsync(CancellationToken ct = default);
}

public sealed class FleetRetentionService : IFleetRetentionService
{
    private readonly IAgentFleetIndexStore _fleetIndex;
    private readonly IFleetSessionSearchService _search;
    private readonly IHermesMemoryStore? _memory;
    private readonly FleetRetentionOptions _options;
    private readonly AgentFleetOptions _fleetOptions;
    private readonly ILogger<FleetRetentionService> _logger;

    public FleetRetentionService(
        IAgentFleetIndexStore fleetIndex,
        IFleetSessionSearchService search,
        IOptions<FleetRetentionOptions> options,
        IOptions<AgentFleetOptions> fleetOptions,
        ILogger<FleetRetentionService> logger,
        IHermesMemoryStore? memory = null)
    {
        _fleetIndex = fleetIndex;
        _search = search;
        _memory = memory;
        _options = options.Value;
        _fleetOptions = fleetOptions.Value;
        _logger = logger;
    }

    public async Task<FleetRetentionSweepResult> ApplyRetentionAsync(CancellationToken ct = default)
    {
        var archiveCutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.FleetIndexArchiveAfterDays));
        var artifactCutoff = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RunArtifactsDeleteAfterDays));

        var entries = await _fleetIndex.ListAsync(
            new AgentFleetListQuery(IncludeArchived: true, Limit: 500),
            ct).ConfigureAwait(false);

        var archivedCount = 0;
        var artifactsPurged = 0;

        foreach (var entry in entries)
        {
            if (entry.Pinned)
                continue;

            if (!entry.Archived
                && IsTerminal(entry.Status)
                && entry.LastActivityAtUtc < archiveCutoff)
            {
                await _fleetIndex.PatchAsync(
                    entry.RunId,
                    new AgentFleetPatchRequest(Archived: true, Actor: "retention-sweep"),
                    ct).ConfigureAwait(false);
                archivedCount++;
                continue;
            }

            if (entry.Archived && entry.LastActivityAtUtc < artifactCutoff)
            {
                var runDir = Path.Combine(Path.GetFullPath(_fleetOptions.RunsRoot), entry.RunId.ToString("D"));
                if (Directory.Exists(runDir))
                {
                    await _search.RemoveAsync(entry.RunId, ct).ConfigureAwait(false);
                    Directory.Delete(runDir, recursive: true);
                    artifactsPurged++;
                }
            }
        }

        var episodicPruned = 0;
        if (_memory is not null)
            episodicPruned = await _memory.PruneExpiredEpisodicAsync(ct).ConfigureAwait(false);

        if (archivedCount > 0 || artifactsPurged > 0 || episodicPruned > 0)
        {
            _logger.LogInformation(
                "Fleet retention sweep: archived={Archived}, artifactsPurged={Artifacts}, episodicPruned={Episodic}",
                archivedCount,
                artifactsPurged,
                episodicPruned);
        }

        return new FleetRetentionSweepResult(archivedCount, artifactsPurged, episodicPruned);
    }

    private static bool IsTerminal(AgentFleetStatus status) =>
        status is AgentFleetStatus.Completed
            or AgentFleetStatus.Failed
            or AgentFleetStatus.Cancelled
            or AgentFleetStatus.HandoffComplete;
}
