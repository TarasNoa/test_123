using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;

namespace Libr4.IDE.Domain.AI;

public enum SuggestionPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class SmartSuggestion : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string SuggestionType { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Dictionary<string, object>? ContextData { get; private set; }
    public Dictionary<string, object> SuggestedAction { get; private set; }
    public SuggestionPriority Priority { get; private set; }
    public float ConfidenceScore { get; private set; }
    public string? Reasoning { get; private set; }
    public bool WasViewed { get; private set; }
    public bool WasAccepted { get; private set; }
    public bool WasDismissed { get; private set; }
    public string? UserFeedback { get; private set; }
    public bool? WasHelpful { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private SmartSuggestion() { }

    public static Result<SmartSuggestion> Create(
        Guid userId,
        string suggestionType,
        string title,
        string description,
        Dictionary<string, object> suggestedAction,
        SuggestionPriority priority = SuggestionPriority.Medium,
        float confidenceScore = 0.0f,
        string? reasoning = null,
        Guid? projectId = null,
        Dictionary<string, object>? contextData = null,
        DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(suggestionType))
            return Result.Failure<SmartSuggestion>(Error.Validation("Suggestion.Type.Required", "Suggestion type is required"));

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<SmartSuggestion>(Error.Validation("Suggestion.Title.Required", "Suggestion title is required"));

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<SmartSuggestion>(Error.Validation("Suggestion.Description.Required", "Suggestion description is required"));

        if (confidenceScore < 0 || confidenceScore > 100)
            return Result.Failure<SmartSuggestion>(Error.Validation("Suggestion.ConfidenceScore.Invalid", "Confidence score must be between 0 and 100"));

        var suggestion = new SmartSuggestion
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SuggestionType = suggestionType,
            Title = title,
            Description = description,
            ProjectId = projectId,
            ContextData = contextData,
            SuggestedAction = suggestedAction,
            Priority = priority,
            ConfidenceScore = confidenceScore,
            Reasoning = reasoning,
            WasViewed = false,
            WasAccepted = false,
            WasDismissed = false,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        suggestion.RaiseDomainEvent(new SmartSuggestionCreatedEvent(suggestion.Id, userId, suggestionType));
        return Result.Success(suggestion);
    }

    public Result MarkAsViewed()
    {
        if (WasViewed)
            return Result.Failure(Error.Validation("Suggestion.AlreadyViewed", "Suggestion already viewed"));

        WasViewed = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SmartSuggestionViewedEvent(Id, UserId));
        return Result.Success();
    }

    public Result Accept(string? feedback = null, bool? helpful = null)
    {
        if (WasAccepted)
            return Result.Failure(Error.Validation("Suggestion.AlreadyAccepted", "Suggestion already accepted"));

        if (WasDismissed)
            return Result.Failure(Error.Validation("Suggestion.InvalidState", "Cannot accept dismissed suggestion"));

        WasAccepted = true;
        WasViewed = true;
        UserFeedback = feedback;
        WasHelpful = helpful;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SmartSuggestionAcceptedEvent(Id, UserId));
        return Result.Success();
    }

    public Result Dismiss(string? feedback = null)
    {
        if (WasDismissed)
            return Result.Failure(Error.Validation("Suggestion.AlreadyDismissed", "Suggestion already dismissed"));

        if (WasAccepted)
            return Result.Failure(Error.Validation("Suggestion.InvalidState", "Cannot dismiss accepted suggestion"));

        WasDismissed = true;
        WasViewed = true;
        UserFeedback = feedback;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new SmartSuggestionDismissedEvent(Id, UserId));
        return Result.Success();
    }

    public Result IsExpired()
    {
        if (ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow)
            return Result.Success();

        return Result.Failure(Error.Validation("Suggestion.NotExpired", "Suggestion not expired"));
    }
}

public record SmartSuggestionCreatedEvent(Guid SuggestionId, Guid UserId, string SuggestionType) : DomainEvent;
public record SmartSuggestionViewedEvent(Guid SuggestionId, Guid UserId) : DomainEvent;
public record SmartSuggestionAcceptedEvent(Guid SuggestionId, Guid UserId) : DomainEvent;
public record SmartSuggestionDismissedEvent(Guid SuggestionId, Guid UserId) : DomainEvent;
