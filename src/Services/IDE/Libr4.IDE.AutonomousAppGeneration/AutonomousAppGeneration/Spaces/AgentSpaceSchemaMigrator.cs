using Libr4.IDE.Application.AutonomousAppGeneration.Spaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class AgentSpaceSchemaMigrator : IHostedService
{
    private readonly ISpaceStore _store;
    private readonly ILogger<AgentSpaceSchemaMigrator> _logger;

    public AgentSpaceSchemaMigrator(ISpaceStore store, ILogger<AgentSpaceSchemaMigrator> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _store.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Agent spaces schema ready");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
