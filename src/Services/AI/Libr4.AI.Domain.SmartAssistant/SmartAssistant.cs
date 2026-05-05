using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.SmartAssistant.Events;

namespace Libr4.AI.Domain.SmartAssistant;

public class SmartAssistantSession : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string SessionType { get; private set; } = string.Empty; // TaskHelp, CareerAdvice, SkillGuidance
    public List<AssistantMessage> Messages { get; private set; } = new();
    public string Context { get; private set; } = string.Empty;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    private SmartAssistantSession() { }

    public void AddMessage(string role, string content, DateTimeOffset now)
    {
        var message = new AssistantMessage
        {
            Id = Guid.NewGuid(),
            Role = role, // user, assistant
            Content = content,
            Timestamp = now
        };
        Messages.Add(message);
        RaiseDomainEvent(new AssistantMessageAddedEvent(Id, UserId, role, now));
    }

    public void EndSession(DateTimeOffset now)
    {
        EndedAt = now;
        RaiseDomainEvent(new AssistantSessionEndedEvent(Id, UserId, SessionType, now));
    }
}

public class AssistantMessage
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty; // user, assistant
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
}

public class AssistantSuggestion
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Suggestion { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public DateTimeOffset SuggestedAt { get; set; }
}
