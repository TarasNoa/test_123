using Libr4.AI.Infrastructure.Exoskeleton;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Exoskeleton Hook - applies verification protocol to prevent hallucinations
/// Based on "Exoskeleton for LLM" pattern
/// </summary>
public class ExoskeletonHook : IHook
{
    private readonly IExoskeletonProtocol _exoskeletonProtocol;
    private readonly ILogger<ExoskeletonHook> _logger;

    public ExoskeletonHook(
        IExoskeletonProtocol exoskeletonProtocol,
        ILogger<ExoskeletonHook> logger)
    {
        _exoskeletonProtocol = exoskeletonProtocol;
        _logger = logger;
    }

    public HookType Type => HookType.PreToolUse;
    public string Name => "Exoskeleton";
    public int Priority => 90; // Very high priority - runs early

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Apply exoskeleton to parameters if they contain a prompt/message
            if (context.Parameters.TryGetValue("message", out var message))
            {
                var enhancedMessage = await _exoskeletonProtocol.ApplyExoskeletonAsync(message?.ToString() ?? string.Empty);
                context.Parameters["message"] = enhancedMessage;
            }
            else if (context.Parameters.TryGetValue("prompt", out var prompt))
            {
                var enhancedPrompt = await _exoskeletonProtocol.ApplyExoskeletonAsync(prompt?.ToString() ?? string.Empty);
                context.Parameters["prompt"] = enhancedPrompt;
            }
            
            _logger.LogDebug("Exoskeleton: Applied verification protocol");
            
            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exoskeleton: Failed to apply protocol");
            return new HookResult { ShouldContinue = true };
        }
    }
}
