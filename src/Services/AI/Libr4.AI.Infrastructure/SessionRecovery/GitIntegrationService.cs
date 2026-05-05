using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.SessionRecovery;

/// <summary>
/// Implementation of git integration service
/// </summary>
public class GitIntegrationService : IGitIntegrationService
{
    private readonly ILogger<GitIntegrationService> _logger;
    private readonly string _projectPath;

    public GitIntegrationService(ILogger<GitIntegrationService> logger)
    {
        _logger = logger;
        _projectPath = Directory.GetCurrentDirectory();
    }

    public async Task<string> CommitChangesAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Add all changes
            await RunGitCommandAsync("add .", cancellationToken);
            
            // Commit with message
            var commitMessage = string.IsNullOrEmpty(message) 
                ? GenerateCommitMessage() 
                : message;
            
            await RunGitCommandAsync($"commit -m \"{commitMessage}\"", cancellationToken);
            
            _logger.LogInformation("Committed changes: {Message}", commitMessage);
            return commitMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit changes");
            throw;
        }
    }

    public async Task<GitStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var output = await RunGitCommandAsync("status --porcelain", cancellationToken);
            var status = ParseGitStatus(output);
            
            // Get current branch
            var branchOutput = await RunGitCommandAsync("branch --show-current", cancellationToken);
            status.CurrentBranch = branchOutput.Trim();
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get git status");
            return new GitStatus();
        }
    }

    public async Task<string> GetDiffAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await RunGitCommandAsync("diff", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get git diff");
            return string.Empty;
        }
    }

    public async Task UndoLastCommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await RunGitCommandAsync("reset --soft HEAD~1", cancellationToken);
            _logger.LogInformation("Undid last commit");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo last commit");
            throw;
        }
    }

    private async Task<string> RunGitCommandAsync(string command, CancellationToken cancellationToken)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                WorkingDirectory = _projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new Exception($"Git command failed: {error}");
        }

        return output;
    }

    private string GenerateCommitMessage()
    {
        // Simple auto-generated commit message
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm");
        return $"AI changes [{timestamp}]";
    }

    private GitStatus ParseGitStatus(string output)
    {
        var status = new GitStatus();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("??"))
                continue;

            var statusCode = line.Substring(0, 2);
            var filePath = line.Substring(3).Trim();

            if (statusCode.StartsWith("M"))
                status.ModifiedFiles.Add(filePath);
            else if (statusCode.StartsWith("A"))
                status.AddedFiles.Add(filePath);
            else if (statusCode.StartsWith("D"))
                status.DeletedFiles.Add(filePath);
        }

        status.HasChanges = status.ModifiedFiles.Any() || status.AddedFiles.Any() || status.DeletedFiles.Any();
        return status;
    }
}
