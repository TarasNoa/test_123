using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIAssistant;

public enum AssistantRole { ProjectManager, Developer, Designer, Recruiter, Consultant, General }
public enum ConversationType { TaskGeneration, TeamFormation, ProjectPlanning, CodeReview, Documentation, GeneralChat }
public enum MessageRole { User, Assistant, System }
public enum WorkflowStatus { Pending, Running, Completed, Failed }
public enum SuggestionPriority { Low, Medium, High, Critical }

public class AIConversation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public ConversationType ConversationType { get; set; } = ConversationType.GeneralChat;
    public AssistantRole AssistantRole { get; set; } = AssistantRole.General;
    public Guid? ProjectId { get; set; }
    public Dictionary<string, object> ContextData { get; set; } = [];
    public string Model { get; set; } = "gpt-4";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2000;
    public int MessagesCount { get; set; }
    public int TokensUsed { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPinned { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }
    public List<AIMessage> Messages { get; set; } = [];
    public List<AIAction> Actions { get; set; } = [];

    public void AddMessage(AIMessage msg)
    {
        Messages.Add(msg);
        MessagesCount++;
        TokensUsed += msg.TokensUsed;
        LastMessageAt = msg.CreatedAt;
    }

    public void Archive() => IsArchived = true;
    public void Pin() => IsPinned = true;
}

public class AIMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Model { get; set; }
    public int TokensUsed { get; set; }
    public int? ResponseTimeMs { get; set; }
    public List<string> Attachments { get; set; } = [];
    public List<string> CodeSnippets { get; set; } = [];
    public int? UserRating { get; set; }
    public bool? IsHelpful { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public void Rate(int rating, bool helpful)
    {
        UserRating = Math.Clamp(rating, 1, 5);
        IsHelpful = helpful;
    }
}

public class AIAction
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? MessageId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, object> ActionData { get; set; } = [];
    public bool WasExecuted { get; set; }
    public Dictionary<string, object>? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RequiresConfirmation { get; set; } = true;
    public bool WasConfirmed { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExecutedAt { get; set; }

    public void Confirm(DateTimeOffset now) { WasConfirmed = true; ConfirmedAt = now; }
    public void Execute(Dictionary<string, object>? result, DateTimeOffset now)
    {
        WasExecuted = true;
        Result = result;
        ExecutedAt = now;
    }
    public void Fail(string error) { ErrorMessage = error; }
}

public class AITemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public List<string> RequiredVariables { get; set; } = [];
    public Dictionary<string, object> ExampleVariables { get; set; } = [];
    public string? RecommendedModel { get; set; }
    public float? RecommendedTemperature { get; set; }
    public int? RecommendedMaxTokens { get; set; }
    public int UsageCount { get; set; }
    public float SuccessRate { get; set; }
    public bool IsPublic { get; set; }
    public Guid? CreatorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void RecordUsage(bool success)
    {
        UsageCount++;
        SuccessRate = ((SuccessRate * (UsageCount - 1)) + (success ? 1 : 0)) / UsageCount;
    }
}

public class AIWorkflow
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<Dictionary<string, object>> Steps { get; set; } = [];
    public int CurrentStep { get; set; }
    public Dictionary<string, object>? InputData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public List<Dictionary<string, object>> StepResults { get; set; } = [];
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;
    public float ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public void Start(DateTimeOffset now) { Status = WorkflowStatus.Running; StartedAt = now; }
    public void Complete(DateTimeOffset now) { Status = WorkflowStatus.Completed; CompletedAt = now; ProgressPercentage = 100; }
    public void Fail(string error) { Status = WorkflowStatus.Failed; ErrorMessage = error; }
    public void AdvanceStep()
    {
        CurrentStep++;
        if (Steps.Count > 0) ProgressPercentage = (float)CurrentStep / Steps.Count * 100;
    }
}

public class SmartSuggestion
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SuggestionType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public Dictionary<string, object> ContextData { get; set; } = [];
    public Dictionary<string, object>? SuggestedAction { get; set; }
    public SuggestionPriority Priority { get; set; } = SuggestionPriority.Medium;
    public float? ConfidenceScore { get; set; }
    public string? Reasoning { get; set; }
    public bool WasViewed { get; set; }
    public bool WasAccepted { get; set; }
    public bool WasDismissed { get; set; }
    public string? UserFeedback { get; set; }
    public bool? WasHelpful { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Accept() { WasAccepted = true; WasViewed = true; }
    public void Dismiss() { WasDismissed = true; WasViewed = true; }
    public bool IsExpired() => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}
