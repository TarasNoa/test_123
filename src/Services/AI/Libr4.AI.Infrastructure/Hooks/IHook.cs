namespace Libr4.AI.Infrastructure.Hooks;

public enum HookType
{
    PreToolUse,
    PostToolUse,
    PreCompact,
    SessionStart,
    SessionEnd
}

public interface IHook
{
    HookType Type { get; }
    string Name { get; }
    Task<HookResult> ExecuteAsync(HookContext context);
}

public class HookResult
{
    public bool ShouldContinue { get; set; } = true;
    public object? ModifiedResult { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
