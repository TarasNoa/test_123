using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public sealed class HermesVectorBackfillHostedService : IHostedService
{
    private readonly IHermesVectorSyncService _sync;
    private readonly QdrantSyncOptions _options;
    private readonly ILogger<HermesVectorBackfillHostedService> _logger;

    public HermesVectorBackfillHostedService(
        IHermesVectorSyncService sync,
        IOptions<QdrantSyncOptions> options,
        ILogger<HermesVectorBackfillHostedService> logger)
    {
        _sync = sync;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.UseQdrantSync)
            return;

        try
        {
            var synced = await _sync.BackfillAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Hermes vector backfill completed ({Count} entries)", synced);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hermes vector backfill failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
