using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public sealed class FileStateCache : IFileStateCache
{
    private sealed record Entry(string Content, DateTime? LastWriteUtc);

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public bool HasRead(string relativePath)
    {
        var key = Normalize(relativePath);
        return _entries.ContainsKey(key);
    }

    public void RecordRead(string relativePath, string content, DateTime? lastWriteUtc = null)
    {
        var key = Normalize(relativePath);
        _entries[key] = new Entry(content, lastWriteUtc);
    }

    public bool IsStale(string relativePath, DateTime lastWriteUtc)
    {
        var key = Normalize(relativePath);
        if (!_entries.TryGetValue(key, out var entry) || entry.LastWriteUtc is null)
            return false;

        return lastWriteUtc > entry.LastWriteUtc.Value;
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
