namespace Libr4.Matching.Domain.Matches;

public class Match
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid FreelancerId { get; private set; }
    public float TotalScore { get; private set; }
    public float KeywordScore { get; private set; }
    public float SemanticScore { get; private set; }
    public IReadOnlyList<string> MatchingSkills { get; private set; }
    public string Explanation { get; private set; }
    public MatchFeedback? Feedback { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FeedbackAt { get; private set; }

    private Match() { }

    public static Match Create(
        Guid taskId,
        Guid freelancerId,
        float totalScore,
        float keywordScore,
        float semanticScore,
        IReadOnlyList<string> matchingSkills,
        string explanation)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            FreelancerId = freelancerId,
            TotalScore = totalScore,
            KeywordScore = keywordScore,
            SemanticScore = semanticScore,
            MatchingSkills = matchingSkills,
            Explanation = explanation,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void RecordFeedback(MatchFeedback feedback)
    {
        Feedback = feedback;
        FeedbackAt = DateTimeOffset.UtcNow;
    }
}

public enum MatchFeedback
{
    Hired = 1,
    Rejected = 2,
    Applied = 3,
    Viewed = 4,
}
