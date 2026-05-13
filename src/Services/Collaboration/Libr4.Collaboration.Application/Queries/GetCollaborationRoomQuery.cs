using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Queries;

public record GetCollaborationRoomQuery(Guid RoomId) : IRequest<Result<CollaborationRoomDto>>;

public class GetCollaborationRoomQueryValidator : AbstractValidator<GetCollaborationRoomQuery>
{
    public GetCollaborationRoomQueryValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");
    }
}

public class GetCollaborationRoomQueryHandler : IRequestHandler<GetCollaborationRoomQuery, Result<CollaborationRoomDto>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public GetCollaborationRoomQueryHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<CollaborationRoomDto>> Handle(GetCollaborationRoomQuery request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure<CollaborationRoomDto>(new Error("Room.NotFound", "Room not found"));
        }

        var dto = new CollaborationRoomDto
        {
            Id = room.Id,
            Name = room.Name,
            RoomType = room.Type.ToString(),
            CreatorId = room.CreatorId,
            TaskId = room.TaskId,
            Description = room.Description,
            IsPublic = room.IsPublic,
            MaxParticipants = room.Settings.MaxParticipants,
            Settings = new Dictionary<string, object>(),
            Status = room.Status.ToString(),
            CreatedAt = room.CreatedAt.DateTime,
            UpdatedAt = room.UpdatedAt.DateTime,
            Participants = room.Sessions.Where(s => s.IsActive).Select(s => new ParticipantDto
            {
                UserId = s.UserId,
                Role = s.Role,
                JoinedAt = s.JoinedAt
            }).ToList(),
            MessageCount = room.Messages.Count,
            ActiveVideoCall = room.VideoCalls.FirstOrDefault(v => v.Status == VideoCallStatus.InProgress) != null
        };

        return dto;
    }
}

public class CollaborationRoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string RoomType { get; set; }
    public Guid CreatorId { get; set; }
    public Guid? TaskId { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public int MaxParticipants { get; set; }
    public Dictionary<string, object> Settings { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ParticipantDto> Participants { get; set; }
    public int MessageCount { get; set; }
    public bool ActiveVideoCall { get; set; }
}

public class ParticipantDto
{
    public Guid UserId { get; set; }
    public string Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
