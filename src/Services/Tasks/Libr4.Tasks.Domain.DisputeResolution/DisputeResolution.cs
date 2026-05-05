using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Tasks.Domain.DisputeResolution.Events;

namespace Libr4.Tasks.Domain.DisputeResolution;

public class Dispute : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid RaisedBy { get; private set; }
    public string DisputeType { get; private set; } = string.Empty; // Quality, Payment, Scope, Communication
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty; // Open, InProgress, Resolved, Escalated
    public string Resolution { get; private set; } = string.Empty;
    public DateTimeOffset RaisedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private Dispute() { }

    public void Raise(string disputeType, string description, DateTimeOffset now)
    {
        DisputeType = disputeType;
        Description = description;
        Status = "Open";
        RaisedAt = now;
        RaiseDomainEvent(new DisputeRaisedEvent(Id, TaskId, RaisedBy, disputeType, now));
    }

    public void Resolve(string resolution, DateTimeOffset now)
    {
        Status = "Resolved";
        Resolution = resolution;
        ResolvedAt = now;
        RaiseDomainEvent(new DisputeResolvedEvent(Id, TaskId, resolution, now));
    }

    public void Escalate(string reason, DateTimeOffset now)
    {
        Status = "Escalated";
        RaiseDomainEvent(new DisputeEscalatedEvent(Id, TaskId, reason, now));
    }
}

public class DisputeEvidence
{
    public Guid Id { get; set; }
    public Guid DisputeId { get; set; }
    public string EvidenceType { get; set; } = string.Empty; // Screenshot, ChatLog, Document, Other
    public string Description { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
}
