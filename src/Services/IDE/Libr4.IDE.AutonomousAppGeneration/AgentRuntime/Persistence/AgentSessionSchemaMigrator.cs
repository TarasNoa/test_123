using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;

public sealed class AgentSessionSchemaMigrator : IHostedService
{
    private readonly IAgentSessionStore _store;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<AgentSessionSchemaMigrator> _logger;

    public AgentSessionSchemaMigrator(
        IAgentSessionStore store,
        IOptions<AgentRuntimeOptions> options,
        ILogger<AgentSessionSchemaMigrator> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableSessionPersistence)
            return;

        await _store.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Agent session SQLite schema ensured at {Path}", _options.SessionDbPath);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
