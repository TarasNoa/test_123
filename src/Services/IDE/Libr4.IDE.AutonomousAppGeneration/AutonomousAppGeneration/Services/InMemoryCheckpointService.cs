using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed class InMemoryCheckpointService : ICheckpointService
{
    private readonly Dictionary<string, CheckpointSnapshot> _snapshots = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    public CheckpointSnapshot CreateSnapshot(Guid runId, string label, IReadOnlyList<GeneratedFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(files);

        var snapshot = new CheckpointSnapshot(
            Id: $"{runId:N}:{Guid.NewGuid():N}",
            Label: label,
            CreatedAtUtc: DateTime.UtcNow,
            FilesByPath: files.ToDictionary(
                f => f.RelativePath,
                f => new GeneratedFile(f.RelativePath, f.Language, f.Content),
                StringComparer.OrdinalIgnoreCase));

        lock (_lock)
        {
            _snapshots[snapshot.Id] = snapshot;
        }

        return snapshot;
    }

    public CheckpointDiff Diff(CheckpointSnapshot baseline, CheckpointSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(current);

        var added = current.FilesByPath.Keys
            .Where(path => !baseline.FilesByPath.ContainsKey(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var removed = baseline.FilesByPath.Keys
            .Where(path => !current.FilesByPath.ContainsKey(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var changed = current.FilesByPath.Keys
            .Where(path => baseline.FilesByPath.TryGetValue(path, out var oldFile)
                && !string.Equals(oldFile.Content, current.FilesByPath[path].Content, StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new CheckpointDiff(added, removed, changed);
    }

    public IReadOnlyList<GeneratedFile> Restore(CheckpointSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.FilesByPath.Values
            .Select(file => new GeneratedFile(file.RelativePath, file.Language, file.Content))
            .ToList();
    }
}
