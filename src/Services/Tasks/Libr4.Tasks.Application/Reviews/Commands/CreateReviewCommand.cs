using FluentValidation;
using Libr4.Tasks.Application.Abstractions;
using Libr4.Tasks.Application.Dtos;
using Libr4.Tasks.Domain;
using Libr4.Tasks.Domain.Reviews;
using Libr4.Tasks.Domain.Tasks;
using TaskStatus = Libr4.Tasks.Domain.Tasks.TaskStatus;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Tasks.Application.Reviews.Commands;

public sealed record CreateReviewCommand(CreateReviewRequest Payload, Guid ReviewerId) : IRequest<Result<ReviewDto>>;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.Payload.TaskId).NotEmpty();
        RuleFor(x => x.Payload.RevieweeId).NotEmpty();
        RuleFor(x => x.Payload.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Payload.Comment).NotEmpty().MinimumLength(10).MaximumLength(1000);
    }
}

public sealed class CreateReviewHandler : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    private readonly ITasksDbContext _db;
    private readonly IClock _clock;

    public CreateReviewHandler(ITasksDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<ReviewDto>> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.Payload.TaskId, ct);
        if (task is null) return Result.Failure<ReviewDto>(TasksErrors.TaskNotFound);

        // Only completed tasks can be reviewed
        if (task.Status != TaskStatus.Completed)
            return Result.Failure<ReviewDto>(Error.Validation("reviews.task_not_completed", "Can only review completed tasks"));

        // Reviewer must be either client or assigned freelancer
        if (task.ClientId != request.ReviewerId && task.AssignedFreelancerId != request.ReviewerId)
            return Result.Failure<ReviewDto>(Error.Forbidden("reviews.not_participant", "Only task participants can leave reviews"));

        // Cannot review yourself
        if (request.Payload.RevieweeId == request.ReviewerId)
            return Result.Failure<ReviewDto>(Error.Validation("reviews.self_review", "Cannot review yourself"));

        // Reviewee must be the other party
        if (request.Payload.RevieweeId != task.ClientId && request.Payload.RevieweeId != task.AssignedFreelancerId)
            return Result.Failure<ReviewDto>(Error.Validation("reviews.invalid_reviewee", "Invalid reviewee"));

        // Check if already reviewed
        var existingReview = await _db.Reviews
            .FirstOrDefaultAsync(r => r.TaskId == request.Payload.TaskId && r.ReviewerId == request.ReviewerId, ct);
        if (existingReview is not null)
            return Result.Failure<ReviewDto>(TasksErrors.ReviewAlreadyExists);

        var review = Review.Create(
            request.Payload.TaskId,
            request.ReviewerId,
            request.Payload.RevieweeId,
            request.Payload.Rating,
            request.Payload.Comment,
            _clock.UtcNow);

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);

        return new ReviewDto(
            review.Id, review.TaskId, review.ReviewerId, "", review.RevieweeId, "",
            review.Rating, review.Comment, review.CreatedAt);
    }
}
