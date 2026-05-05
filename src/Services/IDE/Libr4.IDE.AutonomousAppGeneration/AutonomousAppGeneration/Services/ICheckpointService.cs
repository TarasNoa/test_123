using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

public sealed record CheckpointSnapshot(
    string Id,
    string Label,
    DateTime CreatedAtUtc,
    IReadOnlyDictionary<string, GeneratedFile> FilesByPath);

public sealed record CheckpointDiff(
    IReadOnlyList<string> AddedPaths,
    IReadOnlyList<string> RemovedPaths,
    IReadOnlyList<string> ChangedPaths)
{
    public int TotalChanged => AddedPaths.Count + RemovedPaths.Count + ChangedPaths.Count;
}

public interface ICheckpointService
{
    CheckpointSnapshot CreateSnapshot(Guid runId, string label, IReadOnlyList<GeneratedFile> files);
    CheckpointDiff Diff(CheckpointSnapshot baseline, CheckpointSnapshot current);
    IReadOnlyList<GeneratedFile> Restore(CheckpointSnapshot snapshot);
}
