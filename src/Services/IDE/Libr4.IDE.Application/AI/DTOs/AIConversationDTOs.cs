using Libr4.IDE.Domain.AI;

namespace Libr4.IDE.Application.AI.DTOs;

public record AIConversationDTO(
    Guid Id,
    string Title,
    ConversationType ConversationType,
    AssistantRole AssistantRole,
    Guid? ProjectId,
    string Model,
    int MessagesCount,
    int TokensUsed,
    bool IsArchived,
    bool IsPinned,
    DateTime LastMessageAt,
    DateTime CreatedAt
);

public record AIMessageDTO(
    Guid Id,
    Guid ConversationId,
    MessageRole Role,
    string Content,
    string? Model,
    int TokensUsed,
    int? ResponseTimeMs,
    int? UserRating,
    bool? IsHelpful,
    DateTime CreatedAt
);

public record ChatRequestDTO(
    string Message,
    Guid? ConversationId,
    Dictionary<string, object>? Context
);

public record ChatResponseDTO(
    Guid ConversationId,
    AIMessageDTO Message,
    string Response,
    List<AIActionDTO> SuggestedActions
);

public record AIActionDTO(
    Guid Id,
    string ActionType,
    Dictionary<string, object> ActionData,
    bool RequiresConfirmation
);

public record AITemplateDTO(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    string SystemPrompt,
    string UserPromptTemplate,
    List<string> RequiredVariables,
    Dictionary<string, object> ExampleVariables,
    string? RecommendedModel,
    float? RecommendedTemperature,
    int? RecommendedMaxTokens,
    int UsageCount,
    float SuccessRate,
    bool IsPublic,
    Guid? CreatorId,
    DateTime CreatedAt
);

public record AIWorkflowDTO(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    List<WorkflowStepDTO> Steps,
    int CurrentStep,
    Dictionary<string, object>? InputData,
    Dictionary<string, object>? OutputData,
    Dictionary<string, object>? StepResults,
    WorkflowStatus Status,
    float ProgressPercentage,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt
);

public record WorkflowStepDTO(
    string StepType,
    string Description,
    Dictionary<string, object> Parameters,
    bool RequiresConfirmation
);

public record SmartSuggestionDTO(
    Guid Id,
    Guid UserId,
    string SuggestionType,
    string Title,
    string Description,
    Guid? ProjectId,
    Dictionary<string, object> SuggestedAction,
    SuggestionPriority Priority,
    float ConfidenceScore,
    string? Reasoning,
    bool WasViewed,
    bool WasAccepted,
    bool WasDismissed,
    DateTime? ExpiresAt,
    DateTime CreatedAt
);

public record QualityStatsDTO(
    double AverageScore,
    int TotalScored,
    Dictionary<string, int> ScoreDistribution,
    string ImprovementTrend
);
