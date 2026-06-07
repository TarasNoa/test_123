namespace Libr4.IDE.Application.GitAutomation;

public sealed class ShadowGitCheckpointOptions
{
    public const string SectionName = "AutonomousAppGeneration:ShadowGit";

    public bool Enabled { get; set; } = true;

    public int MaxDiffChars { get; set; } = 8000;

    public string AuthorName { get; set; } = "Libr4 ShadowGit";

    public string AuthorEmail { get; set; } = "shadow@libr4.local";
}

public enum ShadowGitChangeKind
{
    Add,
    Modify,
    Delete,
    Rename
}

public sealed record ShadowGitFileDiff(
    string Path,
    ShadowGitChangeKind ChangeKind,
    string? UnifiedDiff);

public interface IShadowGitCheckpointService
{
    Task EnsureInitializedAsync(string workspacePath, CancellationToken ct = default);

    Task TagRepairAttemptAsync(string workspacePath, int attemptNumber, CancellationToken ct = default);

    Task TagVerifyPassAsync(string workspacePath, int attemptNumber, CancellationToken ct = default);

    Task<string> GetWorkingDiffAsync(string workspacePath, int maxChars, CancellationToken ct = default);

    Task<IReadOnlyList<ShadowGitFileDiff>> GetSnapshotDiffAtTagAsync(
        string workspacePath,
        string tagName,
        CancellationToken ct = default);

    Task<bool> RewindToTagAsync(string workspacePath, string tagName, CancellationToken ct = default);

    static string RepairTagName(int attemptNumber) => $"repair-attempt-{attemptNumber}";

    static string VerifyPassTagName(int attemptNumber) => $"verify-pass-{attemptNumber}";
}
