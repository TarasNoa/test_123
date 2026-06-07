namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;

/// <summary>
/// Tracks in-process background delegation scope. Out-of-process workers still set
/// <c>DELEGATE_BACKGROUND_CHILD=1</c> in their environment.
/// </summary>
public static class DelegationBackgroundContext
{
    private static readonly AsyncLocal<bool> InChildScope = new();

    public static bool IsBackgroundChild =>
        InChildScope.Value
        || string.Equals(
            Environment.GetEnvironmentVariable("DELEGATE_BACKGROUND_CHILD"),
            "1",
            StringComparison.OrdinalIgnoreCase);

    public static BackgroundChildScope EnterChildScope() => new();

    public sealed class BackgroundChildScope : IDisposable
    {
        private bool _disposed;

        internal BackgroundChildScope() => InChildScope.Value = true;

        public void Dispose()
        {
            if (_disposed)
                return;

            InChildScope.Value = false;
            _disposed = true;
        }
    }
}
