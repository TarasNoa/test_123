using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libr4.AI.Application.Abstractions;
using FSharpAlgorithms = Libr4.AI.Domain.TaskRecommendations.Algorithms;

namespace Libr4.AI.Application;

public class TaskRecommendationService : ITaskRecommendationService
{
    public Task<List<TaskRecommendationResult>> RecommendTasksAsync(
        TaskRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.UserProfile == null || request.AvailableTasks == null)
            return Task.FromResult(new List<TaskRecommendationResult>());

        var fsProfile = new FSharpAlgorithms.UserProfileSummary
        {
            UserId = request.UserProfile.UserId,
            Skills = request.UserProfile.Skills.ToArray(),
            Interests = request.UserProfile.Interests.ToArray(),
            AverageRating = (double)request.UserProfile.AverageRating,
            CompletedTasks = request.UserProfile.CompletedTasks
        };

        var fsTasks = request.AvailableTasks
            .Select(t => new FSharpAlgorithms.TaskBrief
            {
                TaskId = t.TaskId,
                Title = t.Title,
                Category = t.Category,
                RequiredSkills = t.RequiredSkills.ToArray(),
                EstimatedHours = t.EstimatedHours,
                Description = t.Description
            })
            .ToArray();

        var fsResults = FSharpAlgorithms.TaskRecommendationAlgorithms.recommendTasks(fsProfile, fsTasks);

        var results = fsResults
            .Select(r => new TaskRecommendationResult(
                TaskId: r.TaskId,
                Title: r.Title,
                MatchScore: r.MatchScore,
                MatchingSkills: r.MatchingSkills.ToList(),
                Reason: r.Reason))
            .ToList();

        return Task.FromResult(results);
    }
}