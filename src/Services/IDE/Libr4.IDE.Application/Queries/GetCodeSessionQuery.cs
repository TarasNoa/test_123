namespace Libr4.IDE.Application.Queries;

using MediatR;
using Libr4.IDE.Application.DTOs;
using Libr4.IDE.Domain;

public record GetCodeSessionQuery(Guid SessionId) : IRequest<CodeSessionDto?>;

public class GetCodeSessionQueryHandler : IRequestHandler<GetCodeSessionQuery, CodeSessionDto?>
{
    private readonly ICodeSessionRepository _repository;

    public GetCodeSessionQueryHandler(ICodeSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<CodeSessionDto?> Handle(GetCodeSessionQuery request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
            return null;

        return new CodeSessionDto(
            session.Id,
            session.Title,
            session.Description,
            session.Language,
            session.ProjectId,
            session.CreatorId,
            session.CreatedAt,
            session.LastActivityAt,
            session.IsActive,
            session.Files.Select(f => new CodeFileDto(
                f.Id,
                f.FileName,
                f.Content,
                f.Language,
                f.CreatedAt,
                f.ModifiedAt
            )).ToList(),
            session.Participants.Select(p => new ParticipantDto(
                p.Id,
                p.UserId,
                p.Role,
                p.JoinedAt,
                p.LeftAt,
                p.IsActive
            )).ToList()
        );
    }
}
