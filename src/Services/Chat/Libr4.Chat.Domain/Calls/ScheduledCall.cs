using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Chat.Domain.Calls;

public class ScheduledCall : Entity<Guid>
{
    public Guid ServerId { get; private set; }
    public Guid ChannelId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public CallType Type { get; private set; }
    public Guid OrganizerId { get; private set; }
    public CallStatus Status { get; private set; }
    public List<CallRecording> Recordings { get; private set; } = new();
    public List<Guid> InvitedUsers { get; private set; } = new();
    public int MaxParticipants { get; private set; } = 0; // 0 = unlimited

    private ScheduledCall() { }

    public static ScheduledCall Create(Guid serverId, string title, DateTimeOffset scheduledAt, CallType type, Guid organizerId, string description = "", List<Guid>? invitedUsers = null)
    {
        return new ScheduledCall
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            Title = title,
            ScheduledAt = scheduledAt,
            Type = type,
            OrganizerId = organizerId,
            Description = description,
            Status = CallStatus.Scheduled,
            InvitedUsers = invitedUsers ?? new List<Guid>()
        };
    }

    public void Start()
    {
        Status = CallStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void End()
    {
        Status = CallStatus.Ended;
        EndedAt = DateTimeOffset.UtcNow;
    }

    public void AddRecording(string url, long size)
    {
        Recordings.Add(new CallRecording(Guid.NewGuid(), url, size, DateTimeOffset.UtcNow));
    }

    public void InviteUser(Guid userId)
    {
        if (!InvitedUsers.Contains(userId))
        {
            InvitedUsers.Add(userId);
        }
    }
}

public record CallRecording(Guid Id, string Url, long Size, DateTimeOffset RecordedAt);