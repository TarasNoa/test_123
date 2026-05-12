using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.Chat.Domain.Calls.Events;

namespace Libr4.Chat.Domain.Calls;

public class Call : AggregateRoot<Guid>
{
    public Guid ChatId { get; private set; }
    public Guid InitiatorId { get; private set; }
    public CallType Type { get; private set; }
    public CallStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public List<CallParticipant> Participants { get; private set; } = new();

    private Call() { }

    public static Call Initiate(Guid chatId, Guid initiatorId, CallType type)
    {
        var call = new Call
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            InitiatorId = initiatorId,
            Type = type,
            Status = CallStatus.Ringing,
            StartedAt = DateTimeOffset.UtcNow
        };

        call.RaiseDomainEvent(new CallInitiatedEvent(call.Id, chatId, initiatorId, type, call.StartedAt));
        return call;
    }

    public void AddParticipant(Guid userId)
    {
        if (!Participants.Any(p => p.UserId == userId))
        {
            Participants.Add(new CallParticipant(userId, CallParticipantStatus.Connected));
        }
    }

    public void EndCall()
    {
        Status = CallStatus.Ended;
        EndedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CallEndedEvent(Id, EndedAt.Value));
    }
}

public enum CallType { Audio, Video }
public enum CallStatus { Scheduled, Ringing, Connected, InProgress, Ended }

public record CallParticipant(Guid UserId, CallParticipantStatus Status);
public enum CallParticipantStatus { Invited, Connected, Disconnected }