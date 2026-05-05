using Libr4.AI.Infrastructure.Harness;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Harness Engineering Hook
/// Provides automatic feedback loops and backpressure for autonomous agent execution
/// Based on "Harness Engineering" pattern from 8 levels of agent engineering
/// </summary>
public class HarnessHook : IHook
{
    private readonly IHarnessEnvironment _harnessEnvironment;
    private readonly ILogger<HarnessHook> _logger;

    public HarnessHook(
        IHarnessEnvironment harnessEnvironment,
        ILogger<HarnessHook> logger)
    {
        _harnessEnvironment = harnessEnvironment;
        _logger = logger;
    }

    public HookType Type => HookType.PostToolUse;
    public string Name => "Harness";
    public int Priority => 80; // High priority for validation

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Only apply harness for code generation/execution tools
            if (!IsCodeTool(context))
                return new HookResult { ShouldContinue = true };

            var code = ExtractCode(context);
            if (string.IsNullOrEmpty(code))
                return new HookResult { ShouldContinue = true };

            // Run quality checks
            var qualityResult = await _harnessEnvironment.CheckQualityAsync(code);
            
            context.Metadata["harness_quality_passed"] = qualityResult.PassesQualityGate.ToString();
            
            if (qualityResult.Issues.Any())
            {
                context.Metadata["harness_quality_issues"] = string.Join("; ", 
                    qualityResult.Issues.Select(i => $"{i.Category}: {i.Description}"));
                
                _logger.LogWarning("Harness: Quality issues found: {Count}", qualityResult.Issues.Count);
            }

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Harness: Failed to apply feedback loops");
            return new HookResult { ShouldContinue = true };
        }
    }

    private bool IsCodeTool(HookContext context)
    {
        var toolName = context.ToolName?.ToLowerInvariant();
        return toolName?.Contains("code") == true 
            || toolName?.Contains("execute") == true
            || toolName?.Contains("sandbox") == true;
    }

    private string? ExtractCode(HookContext context)
    {
        // Try to extract code from context
        if (context.Parameters.TryGetValue("code", out var code))
            return code?.ToString();
        
        if (context.Parameters.TryGetValue("script", out var script))
            return script?.ToString();

        return null;
    }
}
