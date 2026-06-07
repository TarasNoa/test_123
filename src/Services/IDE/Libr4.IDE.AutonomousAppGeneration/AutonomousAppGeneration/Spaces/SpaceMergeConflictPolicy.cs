namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

/// <summary>
/// Space merge conflict resolution policy (7.2.4).
/// By design: conflicts are surfaced to humans — no automatic resolution.
/// </summary>
public static class SpaceMergeConflictPolicy
{
    public const string HumanPromptRequiredReason = "merge_conflict_requires_human_resolution";

    /// <summary>Returns true when merge must stop and wait for human intervention.</summary>
    public static bool RequiresHumanResolution(bool mergeSucceeded, IReadOnlyList<string>? conflicts) =>
        !mergeSucceeded && conflicts is { Count: > 0 };

    public static string FormatHumanReport(IReadOnlyList<string> conflicts) =>
        "Merge blocked — resolve conflicts manually in the integration worktree:\n"
        + string.Join('\n', conflicts.Select(c => $"  • {c}"));
}
