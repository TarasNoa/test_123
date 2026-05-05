namespace Libr4.IDE.Domain.CodeIntelligence;

/// <summary>
/// Entity representing a code suggestion
/// </summary>
public class CodeSuggestion
{
    public Guid Id { get; private set; }
    public string SuggestionText { get; private set; }
    public CompletionType Type { get; private set; }
    public double RelevanceScore { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private CodeSuggestion() { }
    
    public CodeSuggestion(
        string suggestionText,
        CompletionType type,
        double relevanceScore = 1.0,
        string description = "")
    {
        Id = Guid.NewGuid();
        SuggestionText = suggestionText;
        Type = type;
        RelevanceScore = Math.Max(0.0, Math.Min(1.0, relevanceScore));
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetRelevanceScore(double score)
    {
        RelevanceScore = Math.Max(0.0, Math.Min(1.0, score));
    }
    
    public static CodeSuggestion Create(
        string suggestionText,
        CompletionType type,
        double relevanceScore = 1.0,
        string description = "")
    {
        return new CodeSuggestion(suggestionText, type, relevanceScore, description);
    }
}
