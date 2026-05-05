using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Harness;

/// <summary>
/// Implementation of reaction engine for automatic agent lifecycle reactions
/// </summary>
public class ReactionEngine : IReactionEngine
{
    private readonly ILogger<ReactionEngine> _logger;
    private readonly IHarnessEnvironment _harnessEnvironment;
    private ReactionConfiguration _configuration;
    
    public ReactionEngine(
        ILogger<ReactionEngine> logger,
        IHarnessEnvironment harnessEnvironment)
    {
        _logger = logger;
        _harnessEnvironment = harnessEnvironment;
        _configuration = GetDefaultConfiguration();
    }

    public async Task ProcessEventAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken = default)
    {
        var rule = _configuration.Rules.FirstOrDefault(r => r.Event == @event.State && r.Enabled);
        if (rule == null)
            return;

        _logger.LogInformation(
            "Processing reaction for event {Event} from agent {AgentId}, action: {Action}",
            @event.State, @event.AgentId, rule.Action);

        switch (rule.Action)
        {
            case ReactionAction.ForwardLogs:
                await ForwardLogsAsync(@event, cancellationToken);
                break;
            case ReactionAction.ForwardComments:
                await ForwardCommentsAsync(@event, cancellationToken);
                break;
            case ReactionAction.Escalate:
                await EscalateAsync(@event, cancellationToken);
                break;
            case ReactionAction.Retry:
                await RetryAsync(@event, rule.MaxRetries, cancellationToken);
                break;
            case ReactionAction.Notify:
                await NotifyAsync(@event, cancellationToken);
                break;
            case ReactionAction.Abort:
                await AbortAsync(@event, cancellationToken);
                break;
        }
    }

    public async Task<ReactionConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_configuration);
    }

    public async Task UpdateConfigurationAsync(ReactionConfiguration config, CancellationToken cancellationToken = default)
    {
        _configuration = config;
        _logger.LogInformation("Reaction configuration updated");
        await Task.CompletedTask;
    }

    private async Task ForwardLogsAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken)
    {
        // Forward logs to next agent or escalate
        _logger.LogInformation("Forwarding logs for agent {AgentId}", @event.AgentId);
        await Task.CompletedTask;
    }

    private async Task ForwardCommentsAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken)
    {
        // Forward review comments to agent
        _logger.LogInformation("Forwarding comments for agent {AgentId}", @event.AgentId);
        await Task.CompletedTask;
    }

    private async Task EscalateAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken)
    {
        // Escalate to higher-level agent or human
        _logger.LogWarning("Escalating agent {AgentId} due to event {Event}", 
            @event.AgentId, @event.State);
        await Task.CompletedTask;
    }

    private async Task RetryAsync(AgentLifecycleEvent @event, int maxRetries, CancellationToken cancellationToken)
    {
        // Retry the task up to maxRetries times
        var retryCount = @event.Metadata.TryGetValue("retry_count", out var rc) 
            ? Convert.ToInt32(rc) 
            : 0;
        
        if (retryCount < maxRetries)
        {
            _logger.LogInformation("Retrying agent {AgentId}, attempt {RetryCount}/{MaxRetries}", 
                @event.AgentId, retryCount + 1, maxRetries);
            @event.Metadata["retry_count"] = retryCount + 1;
        }
        else
        {
            _logger.LogWarning("Max retries reached for agent {AgentId}", @event.AgentId);
            await EscalateAsync(@event, cancellationToken);
        }
        
        await Task.CompletedTask;
    }

    private async Task NotifyAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken)
    {
        // Send notification to user
        _logger.LogInformation("Notifying about agent {AgentId} event {Event}", 
            @event.AgentId, @event.State);
        await Task.CompletedTask;
    }

    private async Task AbortAsync(AgentLifecycleEvent @event, CancellationToken cancellationToken)
    {
        // Abort the task
        _logger.LogWarning("Aborting agent {AgentId}", @event.AgentId);
        await Task.CompletedTask;
    }

    private ReactionConfiguration GetDefaultConfiguration()
    {
        return new ReactionConfiguration
        {
            Rules = new List<ReactionRule>
            {
                new ReactionRule
                {
                    Event = LifecycleState.CiFailed,
                    Action = ReactionAction.ForwardLogs,
                    MaxRetries = 5,
                    EscalateAfterMinutes = 45,
                    Enabled = true
                },
                new ReactionRule
                {
                    Event = LifecycleState.ChangesRequested,
                    Action = ReactionAction.ForwardComments,
                    MaxRetries = 3,
                    EscalateAfterMinutes = 90,
                    Enabled = true
                },
                new ReactionRule
                {
                    Event = LifecycleState.Stuck,
                    Action = ReactionAction.Escalate,
                    MaxRetries = 0,
                    EscalateAfterMinutes = 10,
                    Enabled = true
                }
            }
        };
    }
}
