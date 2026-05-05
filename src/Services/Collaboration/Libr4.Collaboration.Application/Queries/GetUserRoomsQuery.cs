using FluentValidation;
using Libr4.Collaboration.Domain;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Kernel.Errors;
using MediatR;

namespace Libr4.Collaboration.Application.Queries;

public record GetUserRoomsQuery(Guid UserId, RoomType? RoomType = null, int Limit = 20) : IRequest<Result<List<CollaborationRoomSummaryDto>>>;

public class GetUserRoomsQueryValidator : AbstractValidator<GetUserRoomsQuery>
{
    public GetUserRoomsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Limit cannot exceed 100");
    }
}

public class GetUserRoomsQueryHandler : IRequestHandler<GetUserRoomsQuery, Result<List<CollaborationRoomSummaryDto>>>
{
    private readonly ICollaborationRoomRepository _collaborationRoomRepository;

    public GetUserRoomsQueryHandler(ICollaborationRoomRepository collaborationRoomRepository)
    {
        _collaborationRoomRepository = collaborationRoomRepository;
    }

    public async Task<Result<List<CollaborationRoomSummaryDto>>> Handle(GetUserRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await _collaborationRoomRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var filteredRooms = request.RoomType.HasValue
            ? rooms.Where(r => r.RoomType == request.RoomType.Value).ToList()
            : rooms;

        var limitedRooms = filteredRooms.Take(request.Limit).ToList();

        var dtos = limitedRooms.Select(room => new CollaborationRoomSummaryDto
        {
            Id = room.Id,
            Name = room.Name,
            RoomType = room.RoomType.ToString(),
            Description = room.Description,
            IsPublic = room.IsPublic,
            Status = room.Status.ToString(),
            ParticipantCount = room.Sessions.Count(s => s.IsActive),
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        }).ToList();

        return dtos;
    }
}

public class CollaborationRoomSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string RoomType { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string Status { get; set; }
    public int ParticipantCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
