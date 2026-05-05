namespace Libr4.IDE.Application.SemanticBlame.DTOs;

/// <summary>
/// DTO for CodeEvolution
/// </summary>
public record CodeEvolutionDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public List<GitCommitDto> Commits { get; init; } = new();
    public Dictionary<string, int> ContributorStats { get; init; } = new();
}

/// <summary>
/// DTO for GitCommit
/// </summary>
public record GitCommitDto
{
    public string CommitHash { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTime CommitDate { get; init; }
    public string Message { get; init; } = string.Empty;
}
