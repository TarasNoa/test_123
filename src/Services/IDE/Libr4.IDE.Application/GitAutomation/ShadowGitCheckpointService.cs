using System.Text;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.GitAutomation;

public sealed class ShadowGitCheckpointService : IShadowGitCheckpointService
{
    private readonly ShadowGitCheckpointOptions _options;
    private readonly ILogger<ShadowGitCheckpointService> _logger;

    public ShadowGitCheckpointService(
        IOptions<ShadowGitCheckpointOptions> options,
        ILogger<ShadowGitCheckpointService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task EnsureInitializedAsync(string workspacePath, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (!_options.Enabled)
                return;

            if (Repository.IsValid(workspacePath))
                return;

            Repository.Init(workspacePath);
            using var repo = new Repository(workspacePath);
            WriteDefaultGitIgnore(workspacePath);
            StageAll(repo);
            Commit(repo, "Initial shadow workspace snapshot");
            _logger.LogInformation("Initialized shadow git repo at {Path}", workspacePath);
        }, ct);

    public Task TagRepairAttemptAsync(string workspacePath, int attemptNumber, CancellationToken ct = default) =>
        TagCheckpointAsync(
            workspacePath,
            IShadowGitCheckpointService.RepairTagName(attemptNumber),
            $"Pre-repair attempt {attemptNumber}",
            $"Repair checkpoint attempt {attemptNumber}",
            lightweightTag: false,
            ct);

    public Task TagVerifyPassAsync(string workspacePath, int attemptNumber, CancellationToken ct = default) =>
        TagCheckpointAsync(
            workspacePath,
            IShadowGitCheckpointService.VerifyPassTagName(attemptNumber),
            $"Verify pass attempt {attemptNumber}",
            $"Verify pass checkpoint attempt {attemptNumber}",
            lightweightTag: true,
            ct);

    public Task<IReadOnlyList<ShadowGitFileDiff>> GetSnapshotDiffAtTagAsync(
        string workspacePath,
        string tagName,
        CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (!_options.Enabled || !Repository.IsValid(workspacePath))
                return (IReadOnlyList<ShadowGitFileDiff>)Array.Empty<ShadowGitFileDiff>();

            using var repo = new Repository(workspacePath);
            var tagCommit = ResolveTagCommit(repo, tagName);
            if (tagCommit is null)
                return Array.Empty<ShadowGitFileDiff>();

            var initial = repo.Commits.LastOrDefault();
            if (initial is null || initial.Sha == tagCommit.Sha)
                return Array.Empty<ShadowGitFileDiff>();

            var changes = repo.Diff.Compare<TreeChanges>(initial.Tree, tagCommit.Tree);
            var results = new List<ShadowGitFileDiff>(changes.Count());
            foreach (var change in changes)
            {
                var patch = repo.Diff.Compare<Patch>(
                    initial.Tree,
                    tagCommit.Tree,
                    new[] { change.Path });
                results.Add(new ShadowGitFileDiff(
                    change.Path.Replace('\\', '/'),
                    MapChangeKind(change.Status),
                    string.IsNullOrWhiteSpace(patch.Content) ? null : patch.Content));
            }

            return results;
        }, ct);

    public Task<string> GetWorkingDiffAsync(string workspacePath, int maxChars, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (!_options.Enabled || !Repository.IsValid(workspacePath))
                return string.Empty;

            using var repo = new Repository(workspacePath);
            if (repo.Head?.Tip is null)
                return string.Empty;

            try
            {
                var patch = repo.Diff.Compare<Patch>(
                    repo.Head.Tip.Tree,
                    DiffTargets.Index | DiffTargets.WorkingDirectory);

                return TruncateDiff(patch.Content ?? string.Empty, maxChars);
            }
            catch (LibGit2SharpException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Full shadow git patch failed for {Path}; falling back to status summary",
                    workspacePath);
                return TruncateDiff(BuildFallbackWorkingDiff(repo), maxChars);
            }
        }, ct);

    public Task<bool> RewindToTagAsync(string workspacePath, string tagName, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            if (!_options.Enabled || !Repository.IsValid(workspacePath))
                return false;

            using var repo = new Repository(workspacePath);
            if (repo.Tags[tagName] is null)
            {
                _logger.LogWarning("Shadow git tag {Tag} not found in {Path}", tagName, workspacePath);
                return false;
            }

            LibGit2Sharp.Commands.Checkout(
                repo,
                tagName,
                new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

            _logger.LogInformation("Rewound {Path} to tag {Tag}", workspacePath, tagName);
            return true;
        }, ct);

    private Task TagCheckpointAsync(
        string workspacePath,
        string tagName,
        string commitMessage,
        string tagMessage,
        bool lightweightTag,
        CancellationToken ct) =>
        Task.Run(() =>
        {
            if (!_options.Enabled)
                return;

            if (!Repository.IsValid(workspacePath))
                EnsureInitializedAsync(workspacePath, ct).GetAwaiter().GetResult();

            using var repo = new Repository(workspacePath);
            StageAll(repo);
            Commit(repo, commitMessage);

            if (repo.Tags[tagName] is not null)
            {
                _logger.LogDebug("Tag {Tag} already exists in {Path}", tagName, workspacePath);
                return;
            }

            if (repo.Head?.Tip is null)
                return;

            if (lightweightTag)
                repo.Tags.Add(tagName, repo.Head.Tip);
            else
                repo.Tags.Add(tagName, repo.Head.Tip, CreateSignature(), tagMessage);

            _logger.LogInformation("Tagged {Tag} at {Path}", tagName, workspacePath);
        }, ct);

    private static Commit? ResolveTagCommit(Repository repo, string tagName) =>
        repo.Tags[tagName]?.Target as Commit;

    private static ShadowGitChangeKind MapChangeKind(ChangeKind status) =>
        status switch
        {
            ChangeKind.Added => ShadowGitChangeKind.Add,
            ChangeKind.Deleted => ShadowGitChangeKind.Delete,
            ChangeKind.Renamed => ShadowGitChangeKind.Rename,
            _ => ShadowGitChangeKind.Modify
        };

    private Signature CreateSignature() =>
        new(_options.AuthorName, _options.AuthorEmail, DateTimeOffset.UtcNow);

    private static void StageAll(Repository repo)
    {
        foreach (var item in repo.RetrieveStatus())
        {
            if (!item.State.HasFlag(FileStatus.NewInWorkdir) && !item.State.HasFlag(FileStatus.ModifiedInWorkdir))
                continue;

            var fullPath = Path.Combine(repo.Info.WorkingDirectory, item.FilePath);
            if (Directory.Exists(fullPath))
                continue;

            try
            {
                LibGit2Sharp.Commands.Stage(repo, item.FilePath);
            }
            catch (LibGit2SharpException)
            {
                // Skip paths where git sees a file/directory conflict.
            }
        }
    }

    private static string BuildFallbackWorkingDiff(Repository repo)
    {
        var sb = new StringBuilder();
        foreach (var item in repo.RetrieveStatus())
        {
            if (item.State is FileStatus.Ignored or FileStatus.Unaltered)
                continue;

            sb.Append("--- ").Append(item.FilePath.Replace('\\', '/'))
                .Append(" (").Append(item.State).AppendLine(")");

            var fullPath = Path.Combine(repo.Info.WorkingDirectory, item.FilePath);
            if (!File.Exists(fullPath))
            {
                sb.AppendLine();
                continue;
            }

            try
            {
                var content = File.ReadAllText(fullPath);
                if (content.Length > 2000)
                    content = content[..2000] + "…";
                sb.AppendLine(content);
            }
            catch (IOException)
            {
                sb.AppendLine("(unreadable file)");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string TruncateDiff(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "\n…(truncated)";

    private void Commit(Repository repo, string message)
    {
        try
        {
            repo.Commit(message, CreateSignature(), CreateSignature());
        }
        catch (EmptyCommitException)
        {
            // no changes
        }
    }

    private static void WriteDefaultGitIgnore(string workspacePath)
    {
        var gitignore = Path.Combine(workspacePath, ".gitignore");
        if (File.Exists(gitignore))
            return;

        File.WriteAllText(gitignore, """
            bin/
            obj/
            node_modules/
            .venv/
            __pycache__/
            target/
            .gradle/
            dist/
            build/
            """);
    }
}
