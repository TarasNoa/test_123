namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;

public sealed record DiffHunk(int OldStart, int OldCount, int NewStart, int NewCount, IReadOnlyList<string> Lines);

public sealed record UnifiedDiff(string? TargetPath, IReadOnlyList<DiffHunk> Hunks);

public sealed record PatchApplyResult(
    bool Success,
    string? PatchedContent,
    string? ConflictReport,
    PatchApplyMode Mode);

public enum PatchApplyMode
{
    Exact,
    Fuzzy,
    ThreeWay
}
