using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class DeterministicRunLoggingMiddleware : IRunMiddleware
{
    private readonly ILogger<DeterministicRunLoggingMiddleware> _logger;

    public DeterministicRunLoggingMiddleware(ILogger<DeterministicRunLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public int Order => 100;
    public string Name => "stage_logging";

    public Task OnBeforeStageAsync(AppGenerationOrchestrator orchestrator, string stage, CancellationToken ct)
    {
        _logger.LogDebug("[AutoGen {Id}] middleware.before stage={Stage}", orchestrator.Id, stage);
        return Task.CompletedTask;
    }

    public Task OnAfterStageAsync(
        AppGenerationOrchestrator orchestrator,
        string stage,
        bool succeeded,
        string? detail,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "[AutoGen {Id}] middleware.after stage={Stage} succeeded={Succeeded} detail={Detail}",
            orchestrator.Id,
            stage,
            succeeded,
            detail ?? string.Empty);
        return Task.CompletedTask;
    }
}
