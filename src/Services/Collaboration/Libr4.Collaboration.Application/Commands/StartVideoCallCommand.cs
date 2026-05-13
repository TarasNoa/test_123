using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Commands;

public record StartVideoCallCommand(
    Guid RoomId,
    Guid InitiatorId,
    VideoCallType CallType = VideoCallType.Video,
    Dictionary<string, object>? Settings = null
) : IRequest<Result<Guid>>;

public class StartVideoCallCommandValidator : AbstractValidator<StartVideoCallCommand>
{
    public StartVideoCallCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.InitiatorId)
            .NotEmpty().WithMessage("Initiator ID is required");

        RuleFor(x => x.CallType)
            .Must(type => new[] { VideoCallType.Audio, VideoCallType.Video }.Contains(type))
            .WithMessage("Call type must be one of: Audio, Video");
    }
}

public class StartVideoCallCommandHandler : IRequestHandler<StartVideoCallCommand, Result<Guid>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public StartVideoCallCommandHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<Guid>> Handle(StartVideoCallCommand request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure<Guid>(new Error("Room.NotFound", "Room not found"));
        }

        room.InitiateVideoCall(request.InitiatorId, request.CallType);

        await _collaborationRoomRepository.UpdateAsync(room, cancellationToken);

        return room.ActiveCall!.Id;
    }
}
