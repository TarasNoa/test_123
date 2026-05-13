using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Commands;

public record LeaveCollaborationRoomCommand(
    Guid RoomId,
    Guid UserId
) : IRequest<Result>;

public class LeaveCollaborationRoomCommandValidator : AbstractValidator<LeaveCollaborationRoomCommand>
{
    public LeaveCollaborationRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");
    }
}

public class LeaveCollaborationRoomCommandHandler : IRequestHandler<LeaveCollaborationRoomCommand, Result>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public LeaveCollaborationRoomCommandHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result> Handle(LeaveCollaborationRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure(new Error("Room.NotFound", "Room not found"));
        }

        room.RemoveParticipant(request.UserId);

        await _collaborationRoomRepository.UpdateAsync(room, cancellationToken);

        return Result.Success();
    }
}
