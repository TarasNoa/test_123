using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.AI.Application.Abstractions;

public record FreelancerProfile(
    Guid Id,
    string Name,
    List<string> Skills,
    float Rating,
    int CompletedTasks);

public record OrderAssistantRequest(
    Guid UserId,
    string TaskTitle,
    string Description,
    List<string> RequiredSkills,
    int BudgetMin,
    int BudgetMax,
    int DurationDays,
    List<FreelancerProfile> CandidateFreelancers);

public record OrderAssistantResult(
    int SuggestedBudget,
    int SuggestedDuration,
    List<string> RecommendedFreelancers,
    float Confidence,
    string Reason);

public interface IOrderAssistantService
{
    Task<OrderAssistantResult> SuggestOrderAsync(
        OrderAssistantRequest request,
        CancellationToken cancellationToken = default);
}