using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.GitAutomation;

/// <summary>
/// Stub implementation of Git automation service
/// </summary>
public class GitAutomationService : IGitAutomationService
{
    private readonly ILogger<GitAutomationService> _logger;

    public GitAutomationService(ILogger<GitAutomationService> logger)
    {
        _logger = logger;
    }

    public Task<bool> CloneAsync(string repositoryUrl, string targetPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Cloning {RepositoryUrl} to {TargetPath}", repositoryUrl, targetPath);
        return Task.FromResult(true);
    }

    public Task<bool> CommitAsync(string path, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("Committing in {Path}: {Message}", path, message);
        return Task.FromResult(true);
    }

    public Task<bool> PushAsync(string path, string branch = "main", CancellationToken ct = default)
    {
        _logger.LogInformation("Pushing {Branch} from {Path}", branch, path);
        return Task.FromResult(true);
    }

    public Task<bool> PullAsync(string path, CancellationToken ct = default)
    {
        _logger.LogInformation("Pulling in {Path}", path);
        return Task.FromResult(true);
    }

    public Task<string[]> GetBranchesAsync(string path, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting branches in {Path}", path);
        return Task.FromResult(new[] { "main" });
    }

    public Task<string> GetStatusAsync(string path, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting status in {Path}", path);
        return Task.FromResult("clean");
    }
}
