using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Hooks;

public class HookManager
{
    private readonly Dictionary<HookType, List<IHook>> _hooks;
    private readonly ILogger<HookManager> _logger;

    public HookManager(ILogger<HookManager> logger)
    {
        _logger = logger;
        _hooks = new Dictionary<HookType, List<IHook>>();
    }

    public void RegisterHook(IHook hook)
    {
        if (!_hooks.ContainsKey(hook.Type))
        {
            _hooks[hook.Type] = new List<IHook>();
        }

        _hooks[hook.Type].Add(hook);
        _logger.LogInformation("Registered hook: {HookName} of type {HookType}", hook.Name, hook.Type);
    }

    public void UnregisterHook(string hookName, HookType type)
    {
        if (_hooks.ContainsKey(type))
        {
            var hook = _hooks[type].FirstOrDefault(h => h.Name == hookName);
            if (hook != null)
            {
                _hooks[type].Remove(hook);
                _logger.LogInformation("Unregistered hook: {HookName}", hookName);
            }
        }
    }

    public async Task<HookResult> ExecuteHooksAsync(HookType type, HookContext context)
    {
        if (!_hooks.ContainsKey(type) || _hooks[type].Count == 0)
        {
            return new HookResult { ShouldContinue = true };
        }

        _logger.LogDebug("Executing {Count} hooks of type {HookType}", _hooks[type].Count, type);

        foreach (var hook in _hooks[type])
        {
            try
            {
                var result = await hook.ExecuteAsync(context);

                if (!result.ShouldContinue)
                {
                    _logger.LogWarning("Hook {HookName} stopped execution: {ErrorMessage}", hook.Name, result.ErrorMessage ?? "No error message provided");
                    return result;
                }

                if (result.ModifiedResult != null)
                {
                    context.Result = result.ModifiedResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hook {HookName} threw an exception", hook.Name);
                return new HookResult
                {
                    ShouldContinue = false,
                    ErrorMessage = $"Hook {hook.Name} failed: {ex.Message}"
                };
            }
        }

        return new HookResult { ShouldContinue = true };
    }

    public List<IHook> GetHooks(HookType type)
    {
        return _hooks.ContainsKey(type) ? _hooks[type].ToList() : new List<IHook>();
    }
}
