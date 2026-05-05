namespace Libr4.AI.Infrastructure.SessionRecovery;

/// <summary>
/// Git Integration Service - automatic commits with sensible messages
/// Based on Aider pattern
/// </summary>
public interface IGitIntegrationService
{
    /// <summary>
    /// Commit changes with auto-generated message
    /// </summary>
    Task<string> CommitChangesAsync(string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get git status
    /// </summary>
    Task<GitStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get diff for changes
    /// </summary>
    Task<string> GetDiffAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Undo last commit
    /// </summary>
    Task UndoLastCommitAsync(CancellationToken cancellationToken = default);
}

public class GitStatus
{
    public bool HasChanges { get; set; }
    public List<string> ModifiedFiles { get; set; } = new();
    public List<string> AddedFiles { get; set; } = new();
    public List<string> DeletedFiles { get; set; } = new();
    public string? CurrentBranch { get; set; }
}
