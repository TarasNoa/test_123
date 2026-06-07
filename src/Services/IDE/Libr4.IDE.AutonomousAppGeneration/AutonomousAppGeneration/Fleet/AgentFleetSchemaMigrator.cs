using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Fleet;

public sealed class AgentFleetSchemaMigrator : IHostedService
{
    private readonly IAgentFleetRegistry _registry;
    private readonly ILogger<AgentFleetSchemaMigrator> _logger;

    public AgentFleetSchemaMigrator(IAgentFleetRegistry registry, ILogger<AgentFleetSchemaMigrator> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _registry.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        var count = await _registry.RebuildIndexAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Agent fleet index ready ({Count} runs indexed)", count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
