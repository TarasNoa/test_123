using Libr4.AI.Infrastructure.SessionRecovery;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Git Integration Hook - automatic commits after changes
/// Based on Aider pattern
/// </summary>
public class GitIntegrationHook : IHook
{
    private readonly ILogger<GitIntegrationHook> _logger;
    private readonly IGitIntegrationService _gitIntegration;

    public GitIntegrationHook(
        ILogger<GitIntegrationHook> logger,
        IGitIntegrationService gitIntegration)
    {
        _logger = logger;
        _gitIntegration = gitIntegration;
    }

    public HookType Type => HookType.PostToolUse;
    public string Name => "GitIntegration";
    public int Priority => 10; // Low priority - runs after all other hooks

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Check if there are changes
            var status = await _gitIntegration.GetStatusAsync();
            if (!status.HasChanges)
                return new HookResult { ShouldContinue = true };

            // Auto-commit with AI-generated message
            var message = $"AI agent {context.ToolName} changes";
            await _gitIntegration.CommitChangesAsync(message);

            _logger.LogInformation("GitIntegration: Auto-committed changes for {ToolName}", context.ToolName);
            
            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitIntegration: Failed to auto-commit");
            return new HookResult { ShouldContinue = true };
        }
    }
}
