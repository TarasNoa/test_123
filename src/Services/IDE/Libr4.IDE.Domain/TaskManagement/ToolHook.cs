namespace Libr4.IDE.Domain.TaskManagement;

/// <summary>
/// Hook execution phase (from OpenHarness)
/// </summary>
public enum HookPhase
{
    /// <summary>
    /// Before tool execution
    /// </summary>
    PreToolUse,
    
    /// <summary>
    /// After tool execution
    /// </summary>
    PostToolUse,
    
    /// <summary>
    /// Before command execution
    /// </summary>
    PreCommand,
    
    /// <summary>
    /// After command execution
    /// </summary>
    PostCommand
}

/// <summary>
/// Hook result - can allow, deny, or modify the action
/// </summary>
public class HookResult
{
    public bool ShouldProceed { get; set; }
    public string? DenyReason { get; private set; }
    public Dictionary<string, object> ModifiedParameters { get; set; }
    public string? AdditionalContext { get; set; }
    
    public HookResult(bool shouldProceed, string? denyReason = null)
    {
        ShouldProceed = shouldProceed;
        DenyReason = denyReason;
        ModifiedParameters = new Dictionary<string, object>();
    }
    
    public static HookResult Allow()
    {
        return new HookResult(true);
    }
    
    public static HookResult Deny(string reason)
    {
        return new HookResult(false, reason);
    }
    
    public static HookResult Modify(Dictionary<string, object> modifiedParameters)
    {
        return new HookResult(true) { ModifiedParameters = modifiedParameters };
    }
}

/// <summary>
/// Tool hook for lifecycle events (from OpenHarness PreToolUse/PostToolUse hooks)
/// </summary>
public class ToolHook
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public HookPhase Phase { get; private set; }
    public List<string> TargetTools { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public ToolHook(string name, string description, HookPhase phase, List<string>? targetTools = null, int priority = 0)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Phase = phase;
        TargetTools = targetTools ?? new List<string>();
        Priority = priority;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Execute the hook logic
    /// </summary>
    public HookResult Execute(Dictionary<string, object> parameters, object? context)
    {
        // Hook execution logic would be implemented in application layer
        // This is a domain model for the hook configuration
        return HookResult.Allow();
    }
    
    public void Activate()
    {
        IsActive = true;
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
}

/// <summary>
/// Hook registry for managing all hooks (from OpenHarness hooks system)
/// </summary>
public class HookRegistry
{
    public List<ToolHook> Hooks { get; private set; }
    
    public HookRegistry()
    {
        Hooks = new List<ToolHook>();
    }
    
    public void RegisterHook(ToolHook hook)
    {
        Hooks.Add(hook);
    }
    
    public void UnregisterHook(Guid hookId)
    {
        Hooks.RemoveAll(h => h.Id == hookId);
    }
    
    public List<ToolHook> GetHooksForPhase(HookPhase phase, string? toolName = null)
    {
        return Hooks
            .Where(h => h.Phase == phase && h.IsActive)
            .Where(h => string.IsNullOrEmpty(toolName) || h.TargetTools.Contains(toolName) || h.TargetTools.Count == 0)
            .OrderByDescending(h => h.Priority)
            .ToList();
    }
    
    public HookResult ExecuteHooks(HookPhase phase, Dictionary<string, object> parameters, object? context, string? toolName = null)
    {
        var hooks = GetHooksForPhase(phase, toolName);
        
        foreach (var hook in hooks)
        {
            var result = hook.Execute(parameters, context);
            if (!result.ShouldProceed)
                return result;
            
            // Apply modified parameters if any
            if (result.ModifiedParameters.Count > 0)
            {
                foreach (var kvp in result.ModifiedParameters)
                    parameters[kvp.Key] = kvp.Value;
            }
        }
        
        return HookResult.Allow();
    }
}
