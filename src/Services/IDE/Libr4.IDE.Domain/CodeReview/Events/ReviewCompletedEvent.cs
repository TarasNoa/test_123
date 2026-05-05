using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.CodeReview.Events;

/// <summary>
/// Domain event raised when a review is completed
/// </summary>
public class ReviewCompletedEvent : IDomainEvent
{
    public Guid CodeReviewId { get; }
    public string ReviewId { get; }
    public int IssuesCount { get; }
    public DateTime OccurredOn { get; }
    
    public ReviewCompletedEvent(
        Guid codeReviewId,
        string reviewId,
        int issuesCount)
    {
        CodeReviewId = codeReviewId;
        ReviewId = reviewId;
        IssuesCount = issuesCount;
        OccurredOn = DateTime.UtcNow;
    }
}
