using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class GitWorktreeService : IGitWorktreeService
{
    private readonly ILogger<GitWorktreeService> _logger;

    public GitWorktreeService(ILogger<GitWorktreeService> logger) => _logger = logger;

    public async Task<string> InitOrCloneMainWorktreeAsync(
        string spaceRoot,
        string? repositoryUrl,
        string baseBranch,
        CancellationToken ct = default)
    {
        var mainPath = Path.Combine(spaceRoot, "main");
        Directory.CreateDirectory(spaceRoot);
        EnsurePathWithinSpace(spaceRoot, mainPath);

        if (Directory.Exists(Path.Combine(mainPath, ".git")) ||
            File.Exists(Path.Combine(mainPath, ".git")))
        {
            return mainPath;
        }

        if (!string.IsNullOrWhiteSpace(repositoryUrl))
        {
            await RunGitAsync(null, ct, "clone", "--branch", baseBranch, repositoryUrl, mainPath).ConfigureAwait(false);
            return mainPath;
        }

        Directory.CreateDirectory(mainPath);
        await RunGitAsync(mainPath, ct, "init", "-b", baseBranch).ConfigureAwait(false);
        var readme = Path.Combine(mainPath, "README.md");
        if (!File.Exists(readme))
        {
            await File.WriteAllTextAsync(
                readme,
                "# Agent Space\n\nShared workspace for parallel agents.\n",
                ct).ConfigureAwait(false);
            await RunGitAsync(mainPath, ct, "add", "README.md").ConfigureAwait(false);
            await RunGitAsync(mainPath, ct, "-c", "user.email=space@libr4.local", "-c", "user.name=Libr4 Space", "commit", "-m", "init space").ConfigureAwait(false);
        }

        return mainPath;
    }

    public async Task<GitWorktreeInfo> AddAgentWorktreeAsync(
        string spaceRoot,
        string mainWorktreePath,
        string memberId,
        SpaceMemberRole role,
        string branchName,
        CancellationToken ct = default)
    {
        var wtRoot = Path.Combine(spaceRoot, "worktrees", memberId);
        EnsurePathWithinSpace(spaceRoot, wtRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(wtRoot)!);

        if (Directory.Exists(wtRoot))
            throw new InvalidOperationException($"worktree_already_exists:{memberId}");

        await RunGitAsync(mainWorktreePath, ct, "worktree", "add", "-B", branchName, wtRoot).ConfigureAwait(false);
        var head = await RunGitAsync(wtRoot, ct, "rev-parse", "--short", "HEAD").ConfigureAwait(false);
        _logger.LogInformation("Added worktree {Member} role={Role} branch={Branch}", memberId, role, branchName);
        return new GitWorktreeInfo(wtRoot, branchName, head.Trim());
    }

    public async Task RemoveWorktreeAsync(string spaceRoot, string worktreePath, bool force = false, CancellationToken ct = default)
    {
        EnsurePathWithinSpace(spaceRoot, worktreePath);
        if (!Directory.Exists(worktreePath))
            return;

        var mainPath = Path.Combine(spaceRoot, "main");
        var args = force
            ? new[] { "worktree", "remove", "--force", worktreePath }
            : new[] { "worktree", "remove", worktreePath };
        await RunGitAsync(mainPath, ct, args).ConfigureAwait(false);
    }

    public async Task<GitMergeReport> MergeBranchAsync(
        string integrationWorktreePath,
        string sourceBranch,
        string integrationBranch,
        CancellationToken ct = default)
    {
        await RunGitAsync(integrationWorktreePath, ct, "checkout", integrationBranch).ConfigureAwait(false);
        try
        {
            var output = await RunGitAsync(integrationWorktreePath, ct, "merge", "--no-ff", sourceBranch, "-m", $"merge {sourceBranch} into {integrationBranch}").ConfigureAwait(false);
            return new GitMergeReport(true, output, Array.Empty<string>());
        }
        catch (GitCommandException ex) when (ex.Output.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase))
        {
            var conflicts = await ListConflictFilesAsync(integrationWorktreePath, ct).ConfigureAwait(false);
            try
            {
                await RunGitAsync(integrationWorktreePath, ct, "merge", "--abort").ConfigureAwait(false);
            }
            catch
            {
                // best-effort abort
            }

            return new GitMergeReport(false, ex.Output, conflicts);
        }
    }

    public async Task<GitMergePreview> PreviewMergeAsync(
        string integrationWorktreePath,
        string sourceBranch,
        string integrationBranch,
        int maxDiffChars = 32_000,
        CancellationToken ct = default)
    {
        var range = $"{integrationBranch}...{sourceBranch}";
        var numstat = await RunGitAsync(integrationWorktreePath, ct, "diff", "--numstat", range).ConfigureAwait(false);
        var stat = await RunGitAsync(integrationWorktreePath, ct, "diff", "--stat", range).ConfigureAwait(false);
        var diff = await RunGitAsync(integrationWorktreePath, ct, "diff", range).ConfigureAwait(false);
        if (diff.Length > maxDiffChars)
            diff = diff[..maxDiffChars] + "\n...[truncated]";

        var files = ParseNumStat(numstat);
        return new GitMergePreview(sourceBranch, integrationBranch, files, stat.Trim(), diff.Trim());
    }

    private static IReadOnlyList<GitMergePreviewFile> ParseNumStat(string numstat)
    {
        var files = new List<GitMergePreviewFile>();
        foreach (var line in numstat.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('\t', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
                continue;

            var insertions = parts[0] == "-" ? 0 : int.TryParse(parts[0], out var ins) ? ins : 0;
            var deletions = parts[1] == "-" ? 0 : int.TryParse(parts[1], out var del) ? del : 0;
            var path = parts[2].Replace('\\', '/');
            var kind = insertions > 0 && deletions > 0 ? "modify"
                : insertions > 0 ? "add"
                : deletions > 0 ? "delete"
                : "change";
            files.Add(new GitMergePreviewFile(path, insertions, deletions, kind));
        }

        return files;
    }

    public void EnsurePathWithinSpace(string spaceRoot, string targetPath)
    {
        var root = Path.GetFullPath(spaceRoot);
        var full = Path.GetFullPath(targetPath);
        if (!full.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidOperationException("worktree_path_outside_space_root");
    }

    private async Task<IReadOnlyList<string>> ListConflictFilesAsync(string worktreePath, CancellationToken ct)
    {
        try
        {
            var output = await RunGitAsync(worktreePath, ct, "diff", "--name-only", "--diff-filter=U").ConfigureAwait(false);
            return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private async Task<string> RunGitAsync(string? workingDirectory, CancellationToken ct, params string[] args)
    {
        if (args.Any(a => a.Contains("push", StringComparison.OrdinalIgnoreCase) &&
                          a.Contains("force", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("git_push_force_denied_by_exec_policy");
        }

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("git_not_available");
        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        var combined = string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}\n{stderr}";
        if (process.ExitCode != 0)
            throw new GitCommandException(process.ExitCode, combined);
        return combined;
    }

    private sealed class GitCommandException : Exception
    {
        public GitCommandException(int exitCode, string output) : base($"git exited {exitCode}: {output}")
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; }
        public string Output { get; }
    }
}
