using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Commands;

public record CreateCollaborationRoomCommand(
    string Name,
    CollaborationRoomType RoomType,
    Guid CreatorId,
    Guid? TaskId = null,
    string? Description = null,
    bool IsPublic = false,
    int MaxParticipants = 50,
    Dictionary<string, object>? Settings = null
) : IRequest<Result<Guid>>;

public class CreateCollaborationRoomCommandValidator : AbstractValidator<CreateCollaborationRoomCommand>
{
    public CreateCollaborationRoomCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room name is required")
            .MaximumLength(200).WithMessage("Room name cannot exceed 200 characters");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0).WithMessage("Max participants must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Max participants cannot exceed 100");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

public class CreateCollaborationRoomCommandHandler : IRequestHandler<CreateCollaborationRoomCommand, Result<Guid>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public CreateCollaborationRoomCommandHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<Guid>> Handle(CreateCollaborationRoomCommand request, CancellationToken cancellationToken)
    {
        var room = CollaborationRoom.Create(
            request.Name,
            request.Description ?? string.Empty,
            request.CreatorId,
            request.RoomType
        );

        await _collaborationRoomRepository.AddAsync(room, cancellationToken);

        return room.Id;
    }
}
