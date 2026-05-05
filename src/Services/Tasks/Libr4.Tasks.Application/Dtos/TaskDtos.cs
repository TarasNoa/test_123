namespace Libr4.Tasks.Application.Dtos;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Status,
    Guid ClientId,
    Guid? AssignedFreelancerId,
    decimal Budget,
    string Currency,
    DateTimeOffset? Deadline,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int ApplicationCount);

public sealed record TaskDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Status,
    Guid ClientId,
    string ClientDisplayName,
    Guid? AssignedFreelancerId,
    string? AssignedFreelancerName,
    decimal Budget,
    string Currency,
    DateTimeOffset? Deadline,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ApplicationDto> Applications);

public sealed record ApplicationDto(
    Guid Id,
    Guid TaskId,
    Guid FreelancerId,
    string FreelancerDisplayName,
    string Proposal,
    decimal ProposedBudget,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? RespondedAt);

public sealed record ReviewDto(
    Guid Id,
    Guid TaskId,
    Guid ReviewerId,
    string ReviewerName,
    Guid RevieweeId,
    string RevieweeName,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);

// Requests
public sealed record CreateTaskRequest(string Title, string Description, string Category, decimal Budget, string Currency, DateTimeOffset? Deadline);

public sealed record UpdateTaskRequest(string Title, string Description, string Category, decimal Budget, string Currency, DateTimeOffset? Deadline);

public sealed record ApplyToTaskRequest(string Proposal, decimal ProposedBudget);

public sealed record CreateReviewRequest(Guid TaskId, Guid RevieweeId, int Rating, string Comment);
