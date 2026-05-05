using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.GitAutomation;

/// <summary>
/// Production-ready Git automation using LibGit2Sharp
/// Implements IGitAutomationService with real Git operations
/// </summary>
public class LibGit2SharpService : IGitAutomationService
{
    private readonly ILogger<LibGit2SharpService> _logger;

    public LibGit2SharpService(ILogger<LibGit2SharpService> logger)
    {
        _logger = logger;
    }

    public Task<bool> CloneAsync(string repositoryUrl, string targetPath, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("Cloning {RepositoryUrl} to {TargetPath}", repositoryUrl, targetPath);
                
                Repository.Clone(repositoryUrl, targetPath, new CloneOptions
                {
                    OnCheckoutProgress = (path, completed, total) =>
                    {
                        _logger.LogDebug("Checkout: {Path} ({Completed}/{Total})", path, completed, total);
                    }
                });
                
                _logger.LogInformation("Successfully cloned to {TargetPath}", targetPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clone repository {RepositoryUrl}", repositoryUrl);
                return false;
            }
        }, ct);
    }

    public Task<bool> CommitAsync(string path, string message, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                // Stage all changes
                LibGit2Sharp.Commands.Stage(repo, "*");
                
                // Create commit signature
                var signature = new Signature("Libr4 IDE", "libr4@localhost", DateTimeOffset.Now);
                
                // Commit
                var commit = repo.Commit(message, signature, signature);
                
                _logger.LogInformation("Created commit {CommitSha}: {Message}", commit.Sha[..7], message);
                return true;
            }
            catch (EmptyCommitException)
            {
                _logger.LogInformation("No changes to commit in {Path}", path);
                return true; // Not an error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit in {Path}", path);
                return false;
            }
        }, ct);
    }

    public Task<bool> PushAsync(string path, string branch = "main", CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var remote = repo.Network.Remotes["origin"];
                if (remote == null)
                {
                    _logger.LogError("No remote 'origin' configured in {Path}", path);
                    return false;
                }
                
                var pushRefSpec = $"refs/heads/{branch}";
                repo.Network.Push(remote, pushRefSpec, new PushOptions());
                
                _logger.LogInformation("Pushed {Branch} to origin in {Path}", branch, path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push in {Path}", path);
                return false;
            }
        }, ct);
    }

    public Task<bool> PullAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var signature = new Signature("Libr4 IDE", "libr4@localhost", DateTimeOffset.Now);
                
                var pullOptions = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        OnProgress = progress =>
                        {
                            _logger.LogDebug("Fetch progress: {Progress}", progress);
                            return true;
                        }
                    }
                };
                
                LibGit2Sharp.Commands.Pull(repo, signature, pullOptions);
                
                _logger.LogInformation("Pulled latest changes in {Path}", path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull in {Path}", path);
                return false;
            }
        }, ct);
    }

    public Task<string[]> GetBranchesAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var branches = repo.Branches
                    .Select(b => b.FriendlyName)
                    .ToArray();
                
                return branches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get branches in {Path}", path);
                return Array.Empty<string>();
            }
        }, ct);
    }

    public Task<string> GetStatusAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var status = repo.RetrieveStatus();
                
                var lines = new List<string>();
                
                if (status.Added.Any())
                    lines.Add($"Added: {string.Join(", ", status.Added.Select(s => s.FilePath))}");
                
                if (status.Modified.Any())
                    lines.Add($"Modified: {string.Join(", ", status.Modified.Select(s => s.FilePath))}");
                
                if (status.Removed.Any())
                    lines.Add($"Removed: {string.Join(", ", status.Removed.Select(s => s.FilePath))}");
                
                if (status.Untracked.Any())
                    lines.Add($"Untracked: {string.Join(", ", status.Untracked.Select(s => s.FilePath))}");
                
                if (!lines.Any())
                    lines.Add("Working directory clean");
                
                return string.Join("\n", lines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status in {Path}", path);
                return $"Error: {ex.Message}";
            }
        }, ct);
    }

    // Additional Git operations
    public Task<bool> CreateBranchAsync(string path, string branchName, bool checkout = false, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var newBranch = repo.CreateBranch(branchName);
                
                if (checkout)
                {
                    LibGit2Sharp.Commands.Checkout(repo, newBranch);
                }
                
                _logger.LogInformation("Created branch {BranchName} in {Path}", branchName, path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create branch {BranchName} in {Path}", branchName, path);
                return false;
            }
        }, ct);
    }

    public Task<bool> CheckoutAsync(string path, string branchName, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var branch = repo.Branches[branchName];
                if (branch == null)
                {
                    _logger.LogError("Branch {BranchName} not found in {Path}", branchName, path);
                    return false;
                }
                
                LibGit2Sharp.Commands.Checkout(repo, branch);
                
                _logger.LogInformation("Checked out {BranchName} in {Path}", branchName, path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to checkout {BranchName} in {Path}", branchName, path);
                return false;
            }
        }, ct);
    }

    public Task<bool> FetchAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                foreach (var remote in repo.Network.Remotes)
                {
                    var refSpecs = remote.FetchRefSpecs.Select(rs => rs.Specification);
                    LibGit2Sharp.Commands.Fetch(repo, remote.Name, refSpecs, new FetchOptions(), null);
                }
                
                _logger.LogInformation("Fetched in {Path}", path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch in {Path}", path);
                return false;
            }
        }, ct);
    }

    public Task<bool> MergeAsync(string path, string branchName, string message, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var branch = repo.Branches[branchName];
                if (branch == null)
                {
                    _logger.LogError("Branch {BranchName} not found in {Path}", branchName, path);
                    return false;
                }
                
                var signature = new Signature("Libr4 IDE", "libr4@localhost", DateTimeOffset.Now);
                var result = repo.Merge(branch.Tip, signature, new MergeOptions
                {
                    CommitOnSuccess = true,
                    FastForwardStrategy = FastForwardStrategy.Default
                });
                
                if (result.Status == MergeStatus.Conflicts)
                {
                    _logger.LogWarning("Merge conflicts in {Path}", path);
                    return false;
                }
                
                _logger.LogInformation("Merged {BranchName} in {Path}", branchName, path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to merge {BranchName} in {Path}", branchName, path);
                return false;
            }
        }, ct);
    }

    public Task<string> GetLastCommitMessageAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                
                var commit = repo.Head.Tip;
                return commit?.Message ?? "No commits";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get last commit in {Path}", path);
                return $"Error: {ex.Message}";
            }
        }, ct);
    }

    public Task<string> GetCurrentBranchAsync(string path, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(path);
                return repo.Head.FriendlyName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current branch in {Path}", path);
                return "unknown";
            }
        }, ct);
    }
}
