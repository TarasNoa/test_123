using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Commands;

public record SendMessageCommand(
    Guid RoomId,
    Guid UserId,
    string Message,
    string MessageType = "text",
    Guid? ReplyTo = null,
    Dictionary<string, object>? Metadata = null
) : IRequest<Result<Guid>>;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(10000).WithMessage("Message cannot exceed 10000 characters");

        RuleFor(x => x.MessageType)
            .NotEmpty().WithMessage("Message type is required");
    }
}

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public SendMessageCommandHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure<Guid>(new Error("Room.NotFound", "Room not found"));
        }

        var messageResult = room.AddMessage(request.UserId, request.Message, request.MessageType, request.ReplyTo, request.Metadata);
        if (messageResult.IsFailure)
        {
            return Result.Failure<Guid>(messageResult.Error);
        }

        await _collaborationRoomRepository.UpdateAsync(room, cancellationToken);

        return messageResult.Value.Id;
    }
}
