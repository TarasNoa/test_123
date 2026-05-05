using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.IDEGit;

public enum GitOperationType { Commit, Push, Pull, Branch, Merge, Rebase, Stash, Reset }
public enum MergeStatus { Clean, Conflict, Resolved, Aborted }

public class GitRepository
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public string RepositoryPath { get; set; } = string.Empty;
    public string RemoteUrl { get; set; } = string.Empty;
    public string CurrentBranch { get; set; } = "main";
    public List<string> Branches { get; set; } = new List<string>();
    public DateTimeOffset LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Commit
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public List<string> ChangedFiles { get; set; } = new List<string>();
    public int Additions { get; set; }
    public int Deletions { get; set; }
    public DateTimeOffset CommittedAt { get; set; }
}

public class GitMerge
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public string SourceBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public MergeStatus Status { get; set; }
    public List<string> ConflictFiles { get; set; } = new List<string>();
    public DateTimeOffset CreatedAt { get; set; }
}
