using Libr4.AI.Infrastructure.Compounding;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Compounding Engineering Hook
/// Injects project knowledge base into LLM context for compounding learning
/// Based on pattern: "Plan, Delegate, Evaluate, Codify"
/// </summary>
public class CompoundingHook : IHook
{
    private readonly IProjectKnowledgeBase _knowledgeBase;
    private readonly ILogger<CompoundingHook> _logger;
    private readonly string? _projectPath;

    public CompoundingHook(
        IProjectKnowledgeBase knowledgeBase,
        ILogger<CompoundingHook> logger)
    {
        _knowledgeBase = knowledgeBase;
        _logger = logger;
        _projectPath = Directory.GetCurrentDirectory();
    }

    public HookType Type => HookType.PreToolUse;
    public string Name => "Compounding";
    public int Priority => 50; // Medium priority - runs after SessionRecovery

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(_projectPath))
                return new HookResult { ShouldContinue = true };

            var knowledge = await _knowledgeBase.GetFormattedKnowledgeAsync(_projectPath);
            
            if (!string.IsNullOrEmpty(knowledge))
            {
                context.Metadata["project_knowledge"] = knowledge;
                _logger.LogDebug("Compounding: Injected {Length} chars of project knowledge", knowledge.Length);
            }

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Compounding: Failed to load project knowledge");
            return new HookResult { ShouldContinue = true };
        }
    }
}
