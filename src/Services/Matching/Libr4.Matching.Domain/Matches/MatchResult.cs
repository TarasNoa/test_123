namespace Libr4.Matching.Domain.Matches;

public record MatchResult(
    Guid FreelancerId,
    Guid TaskId,
    float TotalScore,
    float KeywordScore,
    float SemanticScore,
    IReadOnlyList<string> MatchingSkills,
    string Explanation);
