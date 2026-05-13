using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Commands;

public record ShareFileCommand(
    Guid RoomId,
    Guid UserId,
    string FileName,
    long FileSize,
    string FileType,
    string FileUrl,
    string? Description = null
) : IRequest<Result<Guid>>;

public class ShareFileCommandValidator : AbstractValidator<ShareFileCommand>
{
    public ShareFileCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(500).WithMessage("File name cannot exceed 500 characters");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File size must be greater than 0");

        RuleFor(x => x.FileType)
            .NotEmpty().WithMessage("File type is required");

        RuleFor(x => x.FileUrl)
            .NotEmpty().WithMessage("File URL is required");
    }
}

public class ShareFileCommandHandler : IRequestHandler<ShareFileCommand, Result<Guid>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public ShareFileCommandHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<Guid>> Handle(ShareFileCommand request, CancellationToken cancellationToken)
    {
        var room = await _collaborationRoomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room == null)
        {
            return Result.Failure<Guid>(new Error("Room.NotFound", "Room not found"));
        }

        var fileShare = room.ShareFile(
            request.UserId,
            request.FileName,
            request.FileSize,
            request.FileType,
            request.FileUrl,
            request.Description
        );

        await _collaborationRoomRepository.UpdateAsync(room, cancellationToken);

        return fileShare.Id;
    }
}
