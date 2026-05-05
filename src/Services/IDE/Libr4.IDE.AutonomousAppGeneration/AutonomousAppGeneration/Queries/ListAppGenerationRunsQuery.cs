using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record ListAppGenerationRunsQuery : IRequest<IReadOnlyList<RunSummaryDto>>;

public sealed record RunSummaryDto(
    Guid Id,
    string Status,
    string? ApplicationName,
    int Iterations,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? FailureReason,
    string? TenantId);

public sealed class ListAppGenerationRunsQueryHandler
    : IRequestHandler<ListAppGenerationRunsQuery, IReadOnlyList<RunSummaryDto>>
{
    private readonly IAppGenerationRepository _repository;

    public ListAppGenerationRunsQueryHandler(IAppGenerationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<RunSummaryDto>> Handle(
        ListAppGenerationRunsQuery request,
        CancellationToken cancellationToken)
    {
        var runs = await _repository.ListAsync(cancellationToken);
        return runs
            .OrderByDescending(r => r.StartedAt)
            .Select(r => new RunSummaryDto(
                Id: r.Id,
                Status: r.Status.ToString(),
                ApplicationName: r.Plan?.ApplicationName,
                Iterations: r.Iterations.Count,
                StartedAt: r.StartedAt,
                CompletedAt: r.CompletedAt,
                FailureReason: r.FailureReason,
                TenantId: r.TenantId))
            .ToList();
    }
}
