using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Queries;

public record GetRoomMessagesQuery(Guid RoomId, int Limit = 50, Guid? Before = null) : IRequest<Result<List<ChatMessageDto>>>;

public class GetRoomMessagesQueryValidator : AbstractValidator<GetRoomMessagesQuery>
{
    public GetRoomMessagesQueryValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Limit cannot exceed 1000");
    }
}

public class GetRoomMessagesQueryHandler : IRequestHandler<GetRoomMessagesQuery, Result<List<ChatMessageDto>>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public GetRoomMessagesQueryHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<List<ChatMessageDto>>> Handle(GetRoomMessagesQuery request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure<List<ChatMessageDto>>(new Error("Room.NotFound", "Room not found"));
        }

        var messages = room.Messages.AsQueryable();

        if (request.Before.HasValue)
        {
            messages = messages.Where(m => m.Id < request.Before.Value);
        }

        var orderedMessages = messages.OrderByDescending(m => m.SentAt).Take(request.Limit).ToList();

        var dtos = orderedMessages.Select(message => new ChatMessageDto
        {
            Id = message.Id,
            RoomId = message.RoomId,
            SenderId = message.SenderId,
            Content = message.Content,
            Type = message.Type,
            SentAt = message.SentAt
        }).Reverse().ToList();

        return dtos;
    }
}
