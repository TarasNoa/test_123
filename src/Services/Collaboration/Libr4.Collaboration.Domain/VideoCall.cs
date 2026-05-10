using System;
using Libr4.Shared.Kernel.Domain;

namespace Libr4.Collaboration.Domain;

public class VideoCall : Entity<Guid>
{
    public Guid RoomId { get; private set; }
    public Guid InitiatorId { get; private set; }
    public VideoCallType Type { get; private set; }
    public VideoCallStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public List<CallParticipant> Participants { get; private set; } = new();
    public List<CallRecording> Recordings { get; private set; } = new();
    public bool IsRecording { get; private set; }

    private VideoCall() { }

    public static VideoCall Create(Guid roomId, Guid initiatorId, VideoCallType type)
    {
        return new VideoCall
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            InitiatorId = initiatorId,
            Type = type,
            Status = VideoCallStatus.Ringing,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    public void Start()
    {
        Status = VideoCallStatus.InProgress;
    }

    public void End()
    {
        Status = VideoCallStatus.Ended;
        EndedAt = DateTimeOffset.UtcNow;
    }

    public void AddParticipant(Guid userId)
    {
        if (!Participants.Any(p => p.UserId == userId))
        {
            Participants.Add(new CallParticipant(userId, CallParticipantStatus.Connected));
        }
    }

    public void RemoveParticipant(Guid userId)
    {
        Participants.RemoveAll(p => p.UserId == userId);
    }

    public void StartRecording()
    {
        IsRecording = true;
    }

    public void StopRecording(string recordingUrl, long fileSize)
    {
        IsRecording = false;
        Recordings.Add(new CallRecording(Guid.NewGuid(), recordingUrl, fileSize, DateTimeOffset.UtcNow));
    }
}

public enum VideoCallType { Audio, Video, ScreenShare }
public enum VideoCallStatus { Ringing, InProgress, Ended }

public record CallParticipant(Guid UserId, CallParticipantStatus Status);
public enum CallParticipantStatus { Invited, Connected, Disconnected }

public record CallRecording(Guid Id, string Url, long FileSize, DateTimeOffset RecordedAt);
