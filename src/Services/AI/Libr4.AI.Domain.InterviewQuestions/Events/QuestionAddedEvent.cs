using MediatR;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.AI.Domain.InterviewQuestions.Events;

public record QuestionAddedEvent(Guid QuestionSetId, Guid JobId, string Question, string Category, DateTimeOffset AddedAt) : IDomainEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}
