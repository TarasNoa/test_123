namespace Libr4.IDE.Application.Commands;

using MediatR;
using Libr4.IDE.Domain;

public record AddParticipantCommand(
    Guid SessionId,
    Guid UserId,
    string Role = "editor"
) : IRequest;

public class AddParticipantCommandHandler : IRequestHandler<AddParticipantCommand>
{
    private readonly ICodeSessionRepository _repository;

    public AddParticipantCommandHandler(ICodeSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AddParticipantCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
            throw new NotFoundException($"Code session {request.SessionId} not found");

        session.AddParticipant(request.UserId, request.Role);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
