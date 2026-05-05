namespace Libr4.IDE.Domain.GitHubBootstrap;

/// <summary>
/// Value object for template match
/// </summary>
public class TemplateMatch
{
    public GitHubRepo Repository { get; private set; }
    public double MatchScore { get; private set; }
    public string MatchReason { get; private set; }
    
    private TemplateMatch() { }
    
    public TemplateMatch(
        GitHubRepo repository,
        double matchScore,
        string matchReason)
    {
        Repository = repository;
        MatchScore = Math.Max(0.0, Math.Min(1.0, matchScore));
        MatchReason = matchReason;
    }
    
    public static TemplateMatch Create(
        GitHubRepo repository,
        double matchScore,
        string matchReason)
    {
        return new TemplateMatch(repository, matchScore, matchReason);
    }
}
