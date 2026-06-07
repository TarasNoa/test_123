using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed record RunHandoffPromoteMessage(
    Guid SourceRunId,
    string ExportId,
    string BundleSha256,
    string ArtifactPath,
    DateTime EnqueuedAtUtc);

public interface IRunPromoteDispatcher
{
    Task DispatchAsync(RunHandoffPromoteMessage message, CancellationToken ct = default);
}

public interface IRunPromoteService
{
    Task<RunPromoteResult?> PromoteAsync(Guid runId, CancellationToken ct = default);
}

public sealed class RunPromoteService : IRunPromoteService
{
    private readonly IAppGenerationRepository _repository;
    private readonly IRunExportService _export;
    private readonly IRunPromoteDispatcher _dispatcher;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly ILogger<RunPromoteService> _logger;

    public RunPromoteService(
        IAppGenerationRepository repository,
        IRunExportService export,
        IRunPromoteDispatcher dispatcher,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        ILogger<RunPromoteService> logger)
    {
        _repository = repository;
        _export = export;
        _dispatcher = dispatcher;
        _runtimeOptions = runtimeOptions.Value;
        _logger = logger;
    }

    public async Task<RunPromoteResult?> PromoteAsync(Guid runId, CancellationToken ct = default)
    {
        var orchestrator = await _repository.GetAsync(runId, ct).ConfigureAwait(false);
        if (orchestrator is null)
            return null;

        var export = await _export.ExportAsync(runId, ct).ConfigureAwait(false);
        if (export is null)
            return null;

        var message = new RunHandoffPromoteMessage(
            runId,
            export.ExportId,
            export.ContentSha256,
            export.ArtifactPath,
            DateTime.UtcNow);

        await _dispatcher.DispatchAsync(message, ct).ConfigureAwait(false);
        await WritePromoteStateAsync(runId, export, "HandoffPending", ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Promoted run {RunId} to cloud handoff queue export={ExportId} sha={Sha}",
            runId,
            export.ExportId,
            export.ContentSha256);

        return new RunPromoteResult(
            runId,
            runId,
            export.ExportId,
            export.ContentSha256,
            "HandoffPending",
            DateTime.UtcNow);
    }

    private async Task WritePromoteStateAsync(
        Guid runId,
        RunExportResult export,
        string status,
        CancellationToken ct)
    {
        var dir = Path.Combine(Path.GetFullPath(_runtimeOptions.RunsRoot), runId.ToString("D"), "handoff");
        Directory.CreateDirectory(dir);
        var payload = new
        {
            status,
            export.ExportId,
            export.ContentSha256,
            export.ArtifactPath,
            promotedAtUtc = DateTime.UtcNow
        };
        await File.WriteAllTextAsync(
            Path.Combine(dir, "promote-state.json"),
            JsonSerializer.Serialize(payload, RunBundleArchive.JsonOptions()),
            ct).ConfigureAwait(false);
    }
}

public sealed class MassTransitRunPromoteDispatcher : IRunPromoteDispatcher
{
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<MassTransitRunPromoteDispatcher> _logger;

    public MassTransitRunPromoteDispatcher(
        IPublishEndpoint bus,
        ILogger<MassTransitRunPromoteDispatcher> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task DispatchAsync(RunHandoffPromoteMessage message, CancellationToken ct = default)
    {
        await _bus.Publish(message, ct).ConfigureAwait(false);
        _logger.LogInformation(
            "Published run handoff promote message for {RunId} sha={Sha}",
            message.SourceRunId,
            message.BundleSha256);
    }
}

public sealed class NoOpRunPromoteDispatcher : IRunPromoteDispatcher
{
    private readonly ILogger<NoOpRunPromoteDispatcher> _logger;

    public NoOpRunPromoteDispatcher(ILogger<NoOpRunPromoteDispatcher> logger) => _logger = logger;

    public Task DispatchAsync(RunHandoffPromoteMessage message, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Run promote dispatcher not configured; handoff message for {RunId} was not queued",
            message.SourceRunId);
        return Task.CompletedTask;
    }
}

public sealed class RunHandoffPromoteConsumer : IConsumer<RunHandoffPromoteMessage>
{
    private readonly IRunImportService _import;
    private readonly AgentRuntimeOptions _runtimeOptions;
    private readonly ILogger<RunHandoffPromoteConsumer> _logger;

    public RunHandoffPromoteConsumer(
        IRunImportService import,
        IOptions<AgentRuntimeOptions> runtimeOptions,
        ILogger<RunHandoffPromoteConsumer> logger)
    {
        _import = import;
        _runtimeOptions = runtimeOptions.Value;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RunHandoffPromoteMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "Consuming run handoff promote for {RunId} from {Path}",
            message.SourceRunId,
            message.ArtifactPath);

        var result = await _import.ImportBundleAsync(message.ArtifactPath, context.CancellationToken)
            .ConfigureAwait(false);

        var runDir = Path.Combine(
            Path.GetFullPath(_runtimeOptions.RunsRoot),
            message.SourceRunId.ToString("D"),
            "handoff");
        Directory.CreateDirectory(runDir);
        await File.WriteAllTextAsync(
            Path.Combine(runDir, "promote-state.json"),
            JsonSerializer.Serialize(new
            {
                status = "HandoffComplete",
                importedRunId = result.RunId,
                message.BundleSha256,
                completedAtUtc = DateTime.UtcNow
            }, RunBundleArchive.JsonOptions()),
            context.CancellationToken).ConfigureAwait(false);
    }
}
