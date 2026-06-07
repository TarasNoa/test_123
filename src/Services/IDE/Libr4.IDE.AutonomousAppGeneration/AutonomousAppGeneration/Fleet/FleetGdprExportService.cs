using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetGdprExportService
{
    Task<FleetGdprExportBundle?> ExportAsync(Guid runId, CancellationToken ct = default);
}

public sealed record FleetGdprExportBundle(
    Guid RunId,
    string JsonPayload,
    string FileName);

public sealed class FleetGdprExportService : IFleetGdprExportService
{
    private readonly IAgentFleetIndexStore _fleetIndex;
    private readonly IFleetShipStateStore? _shipState;
    private readonly AgentFleetOptions _options;
    private readonly ILogger<FleetGdprExportService> _logger;

    public FleetGdprExportService(
        IAgentFleetIndexStore fleetIndex,
        IOptions<AgentFleetOptions> options,
        ILogger<FleetGdprExportService> logger,
        IFleetShipStateStore? shipState = null)
    {
        _fleetIndex = fleetIndex;
        _shipState = shipState;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FleetGdprExportBundle?> ExportAsync(Guid runId, CancellationToken ct = default)
    {
        var entry = await _fleetIndex.GetAsync(runId, ct).ConfigureAwait(false);
        var runDir = Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"));
        if (entry is null && !Directory.Exists(runDir))
            return null;

        var ship = _shipState is not null ? await _shipState.GetAsync(runId, ct).ConfigureAwait(false) : null;
        var artifacts = Directory.Exists(runDir)
            ? Directory.EnumerateFiles(runDir, "*", SearchOption.AllDirectories)
                .Take(500)
                .Select(p => new
                {
                    path = Path.GetRelativePath(runDir, p).Replace('\\', '/'),
                    bytes = new FileInfo(p).Length
                })
                .ToList()
            : [];

        var payload = new
        {
            exportedAtUtc = DateTime.UtcNow,
            runId,
            fleetEntry = entry,
            shipState = ship,
            artifactCount = artifacts.Count,
            artifacts,
            forkLineage = ReadJsonIfExists(Path.Combine(runDir, "fork", "lineage.json"))
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        _logger.LogInformation("GDPR export bundle created for run {RunId} ({Bytes} bytes)", runId, json.Length);
        return new FleetGdprExportBundle(runId, json, $"libr4-run-{runId:N}-export.json");
    }

    private static object? ReadJsonIfExists(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            return JsonSerializer.Deserialize<object>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }
}
