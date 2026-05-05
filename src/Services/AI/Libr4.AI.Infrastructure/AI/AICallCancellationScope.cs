using System.Threading;

namespace Libr4.AI.Infrastructure.AI;

public static class AICallCancellationScope
{
    private static readonly AsyncLocal<CancellationToken?> _current = new();

    public static CancellationToken Current => _current.Value ?? CancellationToken.None;

    public static IDisposable Push(CancellationToken token)
    {
        var previous = _current.Value;
        _current.Value = token;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly CancellationToken? _previous;
        private bool _disposed;

        public Scope(CancellationToken? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _current.Value = _previous;
            _disposed = true;
        }
    }
}
