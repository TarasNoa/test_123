namespace Libr4.IDE.Domain.GitHubBootstrap;

/// <summary>
/// Entity representing a GitHub repository
/// </summary>
public class GitHubRepo
{
    public Guid Id { get; private set; }
    public string RepoName { get; private set; }
    public string Owner { get; private set; }
    public string Description { get; private set; }
    public LicenseType License { get; private set; }
    public int Stars { get; private set; }
    public string Url { get; private set; }
    public string Language { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private GitHubRepo() { }
    
    public GitHubRepo(
        string repoName,
        string owner,
        string description,
        LicenseType license,
        int stars,
        string url,
        string language = "")
    {
        Id = Guid.NewGuid();
        RepoName = repoName;
        Owner = owner;
        Description = description;
        License = license;
        Stars = stars;
        Url = url;
        Language = language;
        CreatedAt = DateTime.UtcNow;
    }
    
    public static GitHubRepo Create(
        string repoName,
        string owner,
        string description,
        LicenseType license,
        int stars,
        string url,
        string language = "")
    {
        return new GitHubRepo(repoName, owner, description, license, stars, url, language);
    }
}
