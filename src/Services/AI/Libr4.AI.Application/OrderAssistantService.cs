using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Libr4.AI.Application.Abstractions;
using FSharpAlgorithms = Libr4.AI.Domain.OrderAssistant.Algorithms;

namespace Libr4.AI.Application;

public class OrderAssistantService : IOrderAssistantService
{
    public Task<OrderAssistantResult> SuggestOrderAsync(
        OrderAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TaskTitle))
        {
            return Task.FromResult(new OrderAssistantResult(
                SuggestedBudget: 0,
                SuggestedDuration: 1,
                RecommendedFreelancers: new List<string>(),
                Confidence: 0.0f,
                Reason: "Недостаточно данных для анализа."));
        }

        var fsProfiles = (request.CandidateFreelancers ?? new List<FreelancerProfile>())
            .Select(f => new FSharpAlgorithms.FreelancerProfile
            {
                Id = f.Id,
                Name = f.Name,
                Skills = f.Skills.ToArray(),
                Rating = (double)f.Rating,
                CompletedTasks = f.CompletedTasks
            })
            .ToArray();

        var fsResult = FSharpAlgorithms.OrderAssistantAlgorithms.suggestOrder(
            request.TaskTitle,
            request.Description ?? string.Empty,
            (request.RequiredSkills ?? new List<string>()).ToArray(),
            request.BudgetMin,
            request.BudgetMax,
            request.DurationDays,
            fsProfiles);

        var result = new OrderAssistantResult(
            SuggestedBudget: fsResult.SuggestedBudget,
            SuggestedDuration: fsResult.SuggestedDuration,
            RecommendedFreelancers: fsResult.RecommendedFreelancers.ToList(),
            Confidence: fsResult.Confidence,
            Reason: fsResult.Reason);

        return Task.FromResult(result);
    }
}