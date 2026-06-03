using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public interface IAppGenerationRunStarter
{
    Task<AppGenerationRunStartResult> StartInBackgroundAsync(
        StartAppGenerationCommand command,
        CancellationToken ct = default);
}

public sealed record AppGenerationRunStartResult(
    Guid? RunId,
    string Status,
    string Message);

public sealed class AppGenerationRunStarter : IAppGenerationRunStarter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAppGenerationRepository _repository;
    private readonly ILogger<AppGenerationRunStarter> _logger;

    public AppGenerationRunStarter(
        IServiceScopeFactory scopeFactory,
        IAppGenerationRepository repository,
        ILogger<AppGenerationRunStarter> logger)
    {
        _scopeFactory = scopeFactory;
        _repository = repository;
        _logger = logger;
    }

    public async Task<AppGenerationRunStartResult> StartInBackgroundAsync(
        StartAppGenerationCommand command,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserRequest) && command.ResumeFromRunId is null)
            return new AppGenerationRunStartResult(null, "invalid", "userRequest is required");

        var fingerprint = AppGenerationRequestFingerprint.Build(
            command.UserRequest ?? string.Empty,
            command.MaxIterations,
            command.TriggerSource,
            command.TriggerActor,
            command.TenantId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(command, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background app generation run failed to start");
            }
        }, CancellationToken.None);

        var deadline = DateTime.UtcNow.AddSeconds(12);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(400, ct).ConfigureAwait(false);

            var run = await _repository.FindLatestByFingerprintAsync(fingerprint, ct).ConfigureAwait(false);
            if (run is not null)
            {
                return new AppGenerationRunStartResult(
                    run.Id,
                    run.Status.ToString(),
                    "Generation run registered. Poll GET report for progress.");
            }
        }

        return new AppGenerationRunStartResult(
            null,
            "starting",
            "Generation is starting. Refresh run list or poll again shortly.");
    }
}
