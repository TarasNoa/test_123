namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IFleetShipStateStore
{
    Task<RunShipState?> GetAsync(Guid runId, CancellationToken ct = default);
    Task SaveAsync(RunShipState state, CancellationToken ct = default);
}

public sealed class FleetShipStateStore : IFleetShipStateStore
{
    private readonly AgentFleetOptions _options;

    public FleetShipStateStore(Microsoft.Extensions.Options.IOptions<AgentFleetOptions> options)
    {
        _options = options.Value;
    }

    public async Task<RunShipState?> GetAsync(Guid runId, CancellationToken ct = default)
    {
        var path = GetStatePath(runId);
        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return System.Text.Json.JsonSerializer.Deserialize<RunShipState>(json);
    }

    public async Task SaveAsync(RunShipState state, CancellationToken ct = default)
    {
        var path = GetStatePath(state.RunId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = System.Text.Json.JsonSerializer.Serialize(state);
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

    private string GetStatePath(Guid runId) =>
        Path.Combine(Path.GetFullPath(_options.RunsRoot), runId.ToString("D"), "ship", "state.json");
}
