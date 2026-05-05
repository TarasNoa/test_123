namespace Libr4.Tasks.Application.Projects.Dtos;

public sealed record ProjectDto(
    Guid Id,
    string Title,
    string Description,
    string Category,
    string Status,
    Guid OwnerId,
    decimal? BudgetMin,
    decimal? BudgetMax,
    decimal? Budget,
    string Currency,
    string? Client,
    DateTimeOffset? Deadline,
    int TeamSize,
    int Progress,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectMemberDto(
    Guid Id,
    Guid UserId,
    string Role,
    DateTimeOffset JoinedAt);

public sealed record ProjectTaskDto(
    Guid Id,
    string Title,
    string Description,
    Guid? AssignedToId,
    string Status,
    string Priority,
    DateTimeOffset? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record MilestoneDto(
    Guid Id,
    string Title,
    string Description,
    DateTimeOffset DueDate,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
