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
    string CallType = "video",
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
            .NotEmpty().WithMessage("Call type is required")
            .Must(type => new[] { "video", "audio", "screen_share" }.Contains(type))
            .WithMessage("Call type must be one of: video, audio, screen_share");
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

        var videoCallResult = room.StartVideoCall(request.InitiatorId, request.CallType, request.Settings);
        if (videoCallResult.IsFailure)
        {
            return Result.Failure<Guid>(videoCallResult.Error);
        }

        await _collaborationRoomRepository.UpdateAsync(room, cancellationToken);

        return videoCallResult.Value.Id;
    }
}
