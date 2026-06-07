using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentStack;

public interface IAgentStackRunGate
{
    Task EnsureReadyForRunAsync(CancellationToken ct = default);
}

public sealed class AgentStackRunGate : IAgentStackRunGate
{
    private readonly IAgentStackHealthService _health;
    private readonly AgentStackOptions _options;
    private readonly ILogger<AgentStackRunGate> _logger;

    public AgentStackRunGate(
        IAgentStackHealthService health,
        IOptions<AgentStackOptions> options,
        ILogger<AgentStackRunGate> logger)
    {
        _health = health;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureReadyForRunAsync(CancellationToken ct = default)
    {
        if (!_options.RequireHealthyBeforeRun)
            return;

        var status = await _health.CheckAsync(ct).ConfigureAwait(false);
        if (status.AllRequiredHealthy)
            return;

        _logger.LogWarning(
            "Rejecting run — agent stack unhealthy: {Components}",
            string.Join("; ", status.Components.Where(c => !c.Healthy).Select(c => $"{c.Name}={c.Error}")));
        throw new AgentStackUnhealthyException(status);
    }
}
