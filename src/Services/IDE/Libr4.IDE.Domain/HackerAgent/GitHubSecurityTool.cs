namespace Libr4.IDE.Domain.HackerAgent;

/// <summary>
/// Entity representing a GitHub security tool
/// </summary>
public class GitHubSecurityTool
{
    public Guid Id { get; private set; }
    public string RepoName { get; private set; }
    public string RepoUrl { get; private set; }
    public string Description { get; private set; }
    public string ToolType { get; private set; }
    public int Stars { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private GitHubSecurityTool() { }
    
    public GitHubSecurityTool(
        string repoName,
        string repoUrl,
        string description = "",
        string toolType = "",
        int stars = 0)
    {
        Id = Guid.NewGuid();
        RepoName = repoName;
        RepoUrl = repoUrl;
        Description = description;
        ToolType = toolType;
        Stars = stars;
        CreatedAt = DateTime.UtcNow;
    }
    
    public static GitHubSecurityTool Create(
        string repoName,
        string repoUrl,
        string description = "",
        string toolType = "",
        int stars = 0)
    {
        return new GitHubSecurityTool(repoName, repoUrl, description, toolType, stars);
    }
}
