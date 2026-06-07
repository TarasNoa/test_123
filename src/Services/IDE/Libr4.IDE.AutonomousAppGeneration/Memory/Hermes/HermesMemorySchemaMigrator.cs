using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public sealed class HermesMemorySchemaMigrator : IHostedService
{
    private readonly IHermesMemoryStore _store;
    private readonly HermesMemoryOptions _options;
    private readonly ILogger<HermesMemorySchemaMigrator> _logger;

    public HermesMemorySchemaMigrator(
        IHermesMemoryStore store,
        IOptions<HermesMemoryOptions> options,
        ILogger<HermesMemorySchemaMigrator> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        var pruned = await _store.PruneExpiredEpisodicAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Hermes memory SQLite schema ensured at {Path}; pruned {Count} expired episodic rows",
            _options.DbPath,
            pruned);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
