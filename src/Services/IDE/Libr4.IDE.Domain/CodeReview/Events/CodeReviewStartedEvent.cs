using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.CodeReview.Events;

/// <summary>
/// Domain event raised when a code review is started
/// </summary>
public class CodeReviewStartedEvent : IDomainEvent
{
    public Guid CodeReviewId { get; }
    public string ReviewId { get; }
    public DateTime OccurredOn { get; }
    
    public CodeReviewStartedEvent(
        Guid codeReviewId,
        string reviewId)
    {
        CodeReviewId = codeReviewId;
        ReviewId = reviewId;
        OccurredOn = DateTime.UtcNow;
    }
}
