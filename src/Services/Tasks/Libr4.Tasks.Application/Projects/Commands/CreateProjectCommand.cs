using FluentValidation;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Application.Projects.Dtos;
using Libr4.Tasks.Domain.Projects;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MediatR;

namespace Libr4.Tasks.Application.Projects.Commands;

public sealed record CreateProjectCommand(
    string Title,
    string Description,
    string Category,
    Guid ClientId,
    decimal? BudgetMin,
    decimal? BudgetMax,
    string Currency,
    string? Client,
    DateTimeOffset? Deadline
) : IRequest<Result<ProjectDto>>;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(10).MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MinimumLength(50).MaximumLength(5000);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.BudgetMin).GreaterThanOrEqualTo(0).When(x => x.BudgetMin.HasValue);
        RuleFor(x => x.BudgetMax).GreaterThanOrEqualTo(0).When(x => x.BudgetMax.HasValue);
    }
}

public sealed class CreateProjectHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public CreateProjectHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand cmd, CancellationToken ct)
    {
        var project = Project.Create(
            cmd.Title,
            cmd.Description,
            cmd.Category,
            cmd.ClientId,
            cmd.BudgetMin,
            cmd.BudgetMax,
            cmd.Currency,
            cmd.Client,
            cmd.Deadline,
            _clock.UtcNow);

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return MapToDto(project);
    }

    private static ProjectDto MapToDto(Project p) =>
        new(
            p.Id,
            p.Title,
            p.Description,
            p.Category,
            p.Status.ToString(),
            p.OwnerId,
            p.BudgetMin,
            p.BudgetMax,
            p.Budget,
            p.Currency,
            p.Client,
            p.Deadline,
            p.TeamSize,
            p.Progress,
            p.CreatedAt,
            p.UpdatedAt);
}
