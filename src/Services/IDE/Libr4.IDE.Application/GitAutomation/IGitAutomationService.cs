namespace Libr4.IDE.Application.GitAutomation;

/// <summary>
/// Interface for Git automation service
/// </summary>
public interface IGitAutomationService
{
    Task<bool> CloneAsync(string repositoryUrl, string targetPath, CancellationToken ct = default);
    Task<bool> CommitAsync(string path, string message, CancellationToken ct = default);
    Task<bool> PushAsync(string path, string branch = "main", CancellationToken ct = default);
    Task<bool> PullAsync(string path, CancellationToken ct = default);
    Task<string[]> GetBranchesAsync(string path, CancellationToken ct = default);
    Task<string> GetStatusAsync(string path, CancellationToken ct = default);
}
