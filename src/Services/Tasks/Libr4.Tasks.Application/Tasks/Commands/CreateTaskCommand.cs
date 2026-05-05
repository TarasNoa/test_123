using FluentValidation;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Tasks.Domain.Tasks;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Time;
using MediatR;

namespace Libr4.Tasks.Application.Tasks.Commands;

public sealed record CreateTaskCommand(CreateTaskRequest Payload, Guid ClientId) : IRequest<Result<TaskDto>>;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Payload.Title).NotEmpty().MinimumLength(10).MaximumLength(200);
        RuleFor(x => x.Payload.Description).NotEmpty().MinimumLength(50).MaximumLength(5000);
        RuleFor(x => x.Payload.Category).NotEmpty().Must(BeValidCategory).WithMessage("Invalid category");
        RuleFor(x => x.Payload.Budget).GreaterThan(0);
        RuleFor(x => x.Payload.Currency).NotEmpty().Length(3);
    }

    private static bool BeValidCategory(string category) =>
        Enum.TryParse<TaskCategory>(category, true, out _);
}

public sealed class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Result<TaskDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public CreateTaskHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<TaskCategory>(request.Payload.Category, true, out var category))
            return Result.Failure<TaskDto>(TasksErrors.InvalidStatusTransition);

        var task = TaskAggregate.Create(
            request.Payload.Title,
            request.Payload.Description,
            category,
            request.ClientId,
            request.Payload.Budget,
            request.Payload.Currency,
            request.Payload.Deadline,
            _clock.UtcNow);

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        return MapToDto(task);
    }

    private static TaskDto MapToDto(TaskAggregate t) =>
        new(t.Id, t.Title, t.Description, t.Category.ToString(), t.Status.ToString(),
            t.ClientId, t.AssignedFreelancerId, t.Budget, t.Currency, t.Deadline,
            t.CreatedAt, t.UpdatedAt, t.Applications.Count);
}
