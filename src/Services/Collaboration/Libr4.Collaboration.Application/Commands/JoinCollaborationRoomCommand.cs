using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Commands;

public record JoinCollaborationRoomCommand(
    Guid RoomId,
    Guid UserId,
    string Role = "participant"
) : IRequest<Result<Guid>>;

public class JoinCollaborationRoomCommandValidator : AbstractValidator<JoinCollaborationRoomCommand>
{
    public JoinCollaborationRoomCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => new[] { "owner", "admin", "participant", "observer" }.Contains(role))
            .WithMessage("Role must be one of: owner, admin, participant, observer");
    }
}

public class JoinCollaborationRoomCommandHandler : IRequestHandler<JoinCollaborationRoomCommand, Result<Guid>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public JoinCollaborationRoomCommandHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<Guid>> Handle(JoinCollaborationRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure<Guid>(new Error("Room.NotFound", "Room not found"));
        }

        var sessionResult = room.AddParticipant(request.UserId, request.Role);
        if (sessionResult.IsFailure)
        {
            return Result.Failure<Guid>(sessionResult.Error);
        }

        await _collaborationRoomRepository.UpdateAsync(room, cancellationToken);

        return sessionResult.Value.Id;
    }
}
