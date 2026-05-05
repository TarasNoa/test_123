using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.Reviews;

public sealed class Review : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public Guid RevieweeId { get; private set; }
    public int Rating { get; private set; } // 1-5
    public string Comment { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Review() { }

    public static Review Create(
        Guid taskId,
        Guid reviewerId,
        Guid revieweeId,
        int rating,
        string comment,
        DateTimeOffset now)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        var review = new Review
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = now
        };

        review.RaiseDomainEvent(new ReviewSubmittedDomainEvent(taskId, reviewerId, revieweeId, rating));

        return review;
    }

    public void Update(int rating, string comment, DateTimeOffset now)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        Rating = rating;
        Comment = comment.Trim();

        RaiseDomainEvent(new ReviewUpdatedDomainEvent(Id, TaskId, ReviewerId, RevieweeId, rating));
    }
}
