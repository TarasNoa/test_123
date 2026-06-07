using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public interface IRunForkService
{
    Task<RunForkResult?> ForkAsync(Guid sourceRunId, CancellationToken ct = default);
}

public sealed class RunForkService : IRunForkService
{
    private readonly IAppGenerationRepository _repository;
    private readonly IAgentFleetRegistry _fleet;
    private readonly AgentFleetOptions _options;
    private readonly ILogger<RunForkService> _logger;

    public RunForkService(
        IAppGenerationRepository repository,
        IAgentFleetRegistry fleet,
        IOptions<AgentFleetOptions> options,
        ILogger<RunForkService> logger)
    {
        _repository = repository;
        _fleet = fleet;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RunForkResult?> ForkAsync(Guid sourceRunId, CancellationToken ct = default)
    {
        var source = await _repository.GetAsync(sourceRunId, ct).ConfigureAwait(false);
        if (source is null)
            return null;

        var fingerprint = $"{source.RequestFingerprint}-fork-{Guid.NewGuid():N}";
        var fork = AppGenerationOrchestrator.Create(source.UserRequest, fingerprint);
        var planCopied = source.Plan is not null;
        if (planCopied)
            fork.AttachPlan(source.Plan!);

        foreach (var file in source.Files)
            fork.UpsertFile(new GeneratedFile(file.RelativePath, file.Language, file.Content));

        await _repository.SaveAsync(fork, ct).ConfigureAwait(false);
        await WriteLineageAsync(sourceRunId, fork.Id, ct).ConfigureAwait(false);
        await _fleet.UpsertFromRunAsync(fork.Id, ct).ConfigureAwait(false);

        var title = source.Plan?.ApplicationName ?? $"Fork of {sourceRunId.ToString()[..8]}";
        _logger.LogInformation("Forked run {Source} -> {New}", sourceRunId, fork.Id);
        return new RunForkResult(sourceRunId, fork.Id, title, planCopied);
    }

    private async Task WriteLineageAsync(Guid sourceRunId, Guid newRunId, CancellationToken ct)
    {
        var dir = Path.Combine(Path.GetFullPath(_options.RunsRoot), newRunId.ToString("D"), "fork");
        Directory.CreateDirectory(dir);
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            sourceRunId,
            forkedAtUtc = DateTime.UtcNow
        });
        await File.WriteAllTextAsync(Path.Combine(dir, "lineage.json"), payload, ct).ConfigureAwait(false);
    }
}
