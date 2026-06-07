namespace Libr4.AI.Application.Abstractions;

public sealed record LlmCallPreferences(
    string? ModelOverride = null,
    bool DisableStreaming = false);

/// <summary>Async-local LLM call overrides (batch CI profile, run-scoped model routing).</summary>
public static class LlmCallPreferenceContext
{
    private static readonly AsyncLocal<LlmCallPreferences?> Current = new();

    public static LlmCallPreferences? CurrentPreferences => Current.Value;

    public static IDisposable Activate(LlmCallPreferences preferences) => new Scope(preferences);

    private sealed class Scope : IDisposable
    {
        private readonly LlmCallPreferences? _previous;

        public Scope(LlmCallPreferences preferences)
        {
            _previous = Current.Value;
            Current.Value = preferences;
        }

        public void Dispose() => Current.Value = _previous;
    }
}
