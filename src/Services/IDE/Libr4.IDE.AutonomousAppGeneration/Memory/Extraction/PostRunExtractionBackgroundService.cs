using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Extraction;

public sealed class PostRunExtractionBackgroundService : BackgroundService
{
    private readonly IPostRunExtractionQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<PostRunExtractionBackgroundService> _logger;

    public PostRunExtractionBackgroundService(
        IPostRunExtractionQueue queue,
        IServiceProvider services,
        ILogger<PostRunExtractionBackgroundService> logger)
    {
        _queue = queue;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Post-run extraction background service started.");
        try
        {
            await foreach (var runId in _queue.DequeueAllAsync(stoppingToken))
            {
                using var scope = _services.CreateScope();
                var repository = scope.ServiceProvider.GetService<IAppGenerationRepository>();
                var extractor = scope.ServiceProvider.GetService<IPostRunExtractor>();
                if (repository is null || extractor is null)
                {
                    _logger.LogDebug("Post-run extraction skipped for {RunId} (repository/extractor not registered)", runId);
                    continue;
                }

                try
                {
                    var orchestrator = await repository.GetAsync(runId, stoppingToken).ConfigureAwait(false);
                    if (orchestrator is null)
                    {
                        _logger.LogDebug("Post-run extraction skipped; orchestrator {RunId} not found", runId);
                        continue;
                    }

                    await extractor.ExtractAndIngestAsync(orchestrator, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Post-run extraction failed for run {RunId}", runId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
        finally
        {
            _logger.LogInformation("Post-run extraction background service stopped.");
        }
    }
}
