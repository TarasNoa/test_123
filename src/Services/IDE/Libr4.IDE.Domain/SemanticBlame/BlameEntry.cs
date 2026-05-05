namespace Libr4.IDE.Domain.SemanticBlame;

/// <summary>
/// Entity representing a blame entry
/// </summary>
public class BlameEntry
{
    public Guid Id { get; private set; }
    public string FilePath { get; private set; }
    public int LineNumber { get; private set; }
    public string Author { get; private set; }
    public string CommitHash { get; private set; }
    public DateTime CommitDate { get; private set; }
    public string CommitMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private BlameEntry() { }
    
    public BlameEntry(
        string filePath,
        int lineNumber,
        string author,
        string commitHash,
        DateTime commitDate,
        string commitMessage)
    {
        Id = Guid.NewGuid();
        FilePath = filePath;
        LineNumber = lineNumber;
        Author = author;
        CommitHash = commitHash;
        CommitDate = commitDate;
        CommitMessage = commitMessage;
        CreatedAt = DateTime.UtcNow;
    }
    
    public static BlameEntry Create(
        string filePath,
        int lineNumber,
        string author,
        string commitHash,
        DateTime commitDate,
        string commitMessage)
    {
        return new BlameEntry(filePath, lineNumber, author, commitHash, commitDate, commitMessage);
    }
}
