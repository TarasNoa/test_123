using Libr4.IDE.Domain.Common.Events;

namespace Libr4.IDE.Domain.CodeReview.Events;

/// <summary>
/// Risk severity level
/// </summary>
public enum RiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Domain event raised when an issue is detected
/// </summary>
public class IssueDetectedEvent : IDomainEvent
{
    public Guid CodeReviewId { get; }
    public string ReviewId { get; }
    public ReviewType ReviewType { get; }
    public RiskSeverity Severity { get; }
    public DateTime OccurredOn { get; }
    
    public IssueDetectedEvent(
        Guid codeReviewId,
        string reviewId,
        ReviewType reviewType,
        RiskSeverity severity)
    {
        CodeReviewId = codeReviewId;
        ReviewId = reviewId;
        ReviewType = reviewType;
        Severity = severity;
        OccurredOn = DateTime.UtcNow;
    }
}
