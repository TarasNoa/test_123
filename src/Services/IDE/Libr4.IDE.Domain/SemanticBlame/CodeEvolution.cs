namespace Libr4.IDE.Domain.SemanticBlame;

/// <summary>
/// Entity representing code evolution
/// </summary>
public class CodeEvolution
{
    public Guid Id { get; private set; }
    public string FilePath { get; private set; }
    public List<GitCommit> Commits { get; private set; }
    public Dictionary<string, int> ContributorStats { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private CodeEvolution() { }
    
    public CodeEvolution(
        string filePath,
        List<GitCommit>? commits = null,
        Dictionary<string, int>? contributorStats = null)
    {
        Id = Guid.NewGuid();
        FilePath = filePath;
        Commits = commits ?? new List<GitCommit>();
        ContributorStats = contributorStats ?? new Dictionary<string, int>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddCommit(GitCommit commit)
    {
        if (commit != null)
        {
            Commits.Add(commit);
        }
    }
    
    public void UpdateContributorStats(string author)
    {
        if (!string.IsNullOrWhiteSpace(author))
        {
            if (ContributorStats.ContainsKey(author))
            {
                ContributorStats[author]++;
            }
            else
            {
                ContributorStats[author] = 1;
            }
        }
    }
    
    public static CodeEvolution Create(
        string filePath,
        List<GitCommit>? commits = null,
        Dictionary<string, int>? contributorStats = null)
    {
        return new CodeEvolution(filePath, commits, contributorStats);
    }
}

/// <summary>
/// Represents a git commit
/// </summary>
public class GitCommit
{
    public string CommitHash { get; init; }
    public string Author { get; init; }
    public DateTime CommitDate { get; init; }
    public string Message { get; init; }
    
    public GitCommit(
        string commitHash,
        string author,
        DateTime commitDate,
        string message)
    {
        CommitHash = commitHash;
        Author = author;
        CommitDate = commitDate;
        Message = message;
    }
    
    public static GitCommit Create(
        string commitHash,
        string author,
        DateTime commitDate,
        string message)
    {
        return new GitCommit(commitHash, author, commitDate, message);
    }
}
