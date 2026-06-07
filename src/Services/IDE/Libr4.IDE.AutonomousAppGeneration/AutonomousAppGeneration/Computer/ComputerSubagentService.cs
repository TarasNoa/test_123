using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Computer;

public sealed class ComputerSubagentService : IComputerSubagentService
{
    private readonly IComputerFlowRunner _flowRunner;
    private readonly IAgentSpecSubagentRunner _agentRunner;
    private readonly ComputerSubagentOptions _options;
    private readonly ILogger<ComputerSubagentService> _logger;

    public ComputerSubagentService(
        IComputerFlowRunner flowRunner,
        IAgentSpecSubagentRunner agentRunner,
        IOptions<ComputerSubagentOptions> options,
        ILogger<ComputerSubagentService> logger)
    {
        _flowRunner = flowRunner;
        _agentRunner = agentRunner;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ComputerSubagentResult> RunAsync(
        AgentSpec spec,
        string task,
        ToolContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(task);

        if (!_options.Enabled)
        {
            return new ComputerSubagentResult(
                true,
                "computer subagent disabled",
                null,
                false,
                new Dictionary<string, object>());
        }

        var request = ComputerFlowRequestParser.Parse(task, context);
        if (request.HasDeterministicFlow && _flowRunner.CanRun(request.Flow))
        {
            _logger.LogInformation(
                "Running deterministic computer flow {Flow} for run {RunId}",
                request.Flow,
                context.Session.RunId);
            return await _flowRunner.RunAsync(request, context, ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Falling back to agent loop for computer subagent run {RunId}",
            context.Session.RunId);
        var agentResult = await _agentRunner.RunAsync(spec, task, context, ct).ConfigureAwait(false);
        return new ComputerSubagentResult(
            agentResult.Succeeded,
            agentResult.Summary ?? (agentResult.Succeeded ? "computer agent done" : "computer agent failed"),
            null,
            false,
            new Dictionary<string, object>());
    }
}
