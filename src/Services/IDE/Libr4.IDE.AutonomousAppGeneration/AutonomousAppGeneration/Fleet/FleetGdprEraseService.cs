using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetGdprEraseService
{
    Task<FleetGdprEraseResult?> EraseAsync(Guid runId, CancellationToken ct = default);
}

public sealed class FleetGdprEraseService : IFleetGdprEraseService
{
    private readonly IAgentFleetIndexStore _fleetIndex;
    private readonly IFleetSessionSearchService _search;
    private readonly IFleetSimilarRunsService? _similarRuns;
    private readonly IFleetShipStateStore? _shipState;
    private readonly AgentFleetOptions _options;
    private readonly ILogger<FleetGdprEraseService> _logger;

    public FleetGdprEraseService(
        IAgentFleetIndexStore fleetIndex,
        IFleetSessionSearchService search,
        IOptions<AgentFleetOptions> options,
        ILogger<FleetGdprEraseService> logger,
        IFleetShipStateStore? shipState = null,
        IFleetSimilarRunsService? similarRuns = null)
    {
        _fleetIndex = fleetIndex;
        _search = search;
        _options = options.Value;
        _logger = logger;
        _shipState = shipState;
        _similarRuns = similarRuns;
    }

    public async Task<FleetGdprEraseResult?> EraseAsync(Guid runId, CancellationToken ct = default)
    {
        var existing = await _fleetIndex.GetAsync(runId, ct).ConfigureAwait(false);
        var runDir = Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"));
        if (existing is null && !Directory.Exists(runDir))
            return null;

        await _search.RemoveAsync(runId, ct).ConfigureAwait(false);
        if (_similarRuns is not null)
            await _similarRuns.RemoveAsync(runId, ct).ConfigureAwait(false);
        await _fleetIndex.DeleteAsync(runId, ct).ConfigureAwait(false);

        var dirRemoved = false;
        if (Directory.Exists(runDir))
        {
            Directory.Delete(runDir, recursive: true);
            dirRemoved = true;
        }

        _logger.LogWarning("GDPR erase completed for run {RunId}", runId);
        return new FleetGdprEraseResult(runId, existing is not null, true, dirRemoved);
    }
}
