namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;

public interface IFlowProgressStore
{
    Task SaveAsync(FlowProgress progress, CancellationToken ct = default);
    Task<FlowProgress?> LoadAsync(Guid runId, CancellationToken ct = default);
}

public sealed class FileFlowProgressStore : IFlowProgressStore
{
    private readonly FlowEngineOptions _options;

    public FileFlowProgressStore(Microsoft.Extensions.Options.IOptions<FlowEngineOptions> options) =>
        _options = options.Value;

    public async Task SaveAsync(FlowProgress progress, CancellationToken ct = default)
    {
        var dir = RunDir(progress.RunId);
        Directory.CreateDirectory(dir);
        var json = System.Text.Json.JsonSerializer.Serialize(progress);
        await File.WriteAllTextAsync(Path.Combine(dir, "flow-state.json"), json, ct).ConfigureAwait(false);
    }

    public async Task<FlowProgress?> LoadAsync(Guid runId, CancellationToken ct = default)
    {
        var path = Path.Combine(RunDir(runId), "flow-state.json");
        if (!File.Exists(path))
            return null;
        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return System.Text.Json.JsonSerializer.Deserialize<FlowProgress>(json);
    }

    private string RunDir(Guid runId) =>
        Path.Combine(_options.RunsRoot, runId.ToString("D"), "flow");
}
