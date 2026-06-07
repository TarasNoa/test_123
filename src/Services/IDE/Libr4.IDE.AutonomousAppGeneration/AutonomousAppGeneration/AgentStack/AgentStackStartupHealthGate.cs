using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public sealed class AgentStackStartupHealthGate : IHostedService
{
    private readonly IAgentStackHealthService _health;
    private readonly AgentStackOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<AgentStackStartupHealthGate> _logger;

    public AgentStackStartupHealthGate(
        IAgentStackHealthService health,
        IOptions<AgentStackOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<AgentStackStartupHealthGate> logger)
    {
        _health = health;
        _options = options.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.RequireHealthyAtStartup)
            return;

        var status = await _health.CheckAsync(cancellationToken).ConfigureAwait(false);
        if (status.AllRequiredHealthy)
        {
            _logger.LogInformation("Agent stack startup gate passed");
            return;
        }

        _logger.LogCritical(
            "Agent stack startup gate FAILED: {Components}",
            string.Join("; ", status.Components.Where(c => !c.Healthy).Select(c => $"{c.Name}={c.Error}")));
        _lifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
