using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

public class ToolUsageLoggingHook : IHook
{
    private readonly ILogger<ToolUsageLoggingHook> _logger;

    public HookType Type => HookType.PreToolUse;
    public string Name => "ToolUsageLogging";

    public ToolUsageLoggingHook(ILogger<ToolUsageLoggingHook> logger)
    {
        _logger = logger;
    }

    public Task<HookResult> ExecuteAsync(HookContext context)
    {
        _logger.LogInformation(
            "Tool usage: {ToolName}, Session: {SessionId}, Parameters: {Parameters}",
            context.ToolName,
            context.SessionId,
            string.Join(", ", context.Parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))
        );

        return Task.FromResult(new HookResult { ShouldContinue = true });
    }
}
