using Libr4.AI.Infrastructure.Workbench;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks.BuiltIn;

/// <summary>
/// Workbench Hook - injects cross-project knowledge into LLM context
/// Based on "Слепое пятно LLM-разработки: контекст за пределами кода" article
/// </summary>
public class WorkbenchHook : IHook
{
    private readonly IWorkbenchManager _workbenchManager;
    private readonly ILogger<WorkbenchHook> _logger;
    private readonly string? _currentProject;

    public WorkbenchHook(
        IWorkbenchManager workbenchManager,
        ILogger<WorkbenchHook> logger)
    {
        _workbenchManager = workbenchManager;
        _logger = logger;
        
        // Extract project name from current directory
        var currentDir = Directory.GetCurrentDirectory();
        _currentProject = Path.GetFileName(currentDir);
    }

    public HookType Type => HookType.PreToolUse;
    public string Name => "Workbench";
    public int Priority => 40; // Medium-low priority - runs after compounding

    public async Task<HookResult> ExecuteAsync(HookContext context)
    {
        try
        {
            // Initialize workbench if needed
            await _workbenchManager.InitializeAsync();

            // Get workbench context
            var workbenchContext = await _workbenchManager.GetContextAsync(_currentProject);
            
            if (!string.IsNullOrEmpty(workbenchContext))
            {
                context.Metadata["workbench_context"] = workbenchContext;
                context.Metadata["workbench_project"] = _currentProject ?? "unknown";
                
                _logger.LogDebug("Workbench: Injected {Length} chars of cross-project context", workbenchContext.Length);
            }

            return new HookResult { ShouldContinue = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workbench: Failed to load context");
            return new HookResult { ShouldContinue = true };
        }
    }
}
