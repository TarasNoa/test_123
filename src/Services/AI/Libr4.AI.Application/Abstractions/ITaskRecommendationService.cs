using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.AI.Application.Abstractions;

public record TaskBrief(
    Guid TaskId,
    string Title,
    string Category,
    List<string> RequiredSkills,
    int EstimatedHours,
    string Description);

public record UserProfileSummary(
    Guid UserId,
    List<string> Skills,
    List<string> Interests,
    float AverageRating,
    int CompletedTasks);

public record TaskRecommendationRequest(
    UserProfileSummary UserProfile,
    List<TaskBrief> AvailableTasks);

public record TaskRecommendationResult(
    Guid TaskId,
    string Title,
    float MatchScore,
    List<string> MatchingSkills,
    string Reason);

public interface ITaskRecommendationService
{
    Task<List<TaskRecommendationResult>> RecommendTasksAsync(
        TaskRecommendationRequest request,
        CancellationToken cancellationToken = default);
}