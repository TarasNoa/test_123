using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;

public sealed class RepairPlaybookSchemaMigrator : IHostedService
{
    private readonly IRepairPlaybookStore _store;
    private readonly RepairPlaybookOptions _options;
    private readonly ILogger<RepairPlaybookSchemaMigrator> _logger;

    public RepairPlaybookSchemaMigrator(
        IRepairPlaybookStore store,
        IOptions<RepairPlaybookOptions> options,
        ILogger<RepairPlaybookSchemaMigrator> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Repair playbook SQLite schema ensured at {Path}", _options.DbPath);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
