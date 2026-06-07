using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Cognitive;

public sealed class CognitiveMemoryBackfillHostedService : IHostedService
{
    private readonly ICognitiveMemoryBridge _bridge;
    private readonly ILogger<CognitiveMemoryBackfillHostedService> _logger;

    public CognitiveMemoryBackfillHostedService(
        ICognitiveMemoryBridge bridge,
        ILogger<CognitiveMemoryBackfillHostedService> logger)
    {
        _bridge = bridge;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var synced = await _bridge.BackfillFromHermesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Cognitive memory backfill completed ({Count} entries)", synced);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cognitive memory backfill failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
