namespace Libr4.IDE.Application.Commands;

using MediatR;
using Libr4.IDE.Domain;

public record UpdateFileCommand(
    Guid SessionId,
    Guid FileId,
    string Content
) : IRequest;

public class UpdateFileCommandHandler : IRequestHandler<UpdateFileCommand>
{
    private readonly ICodeSessionRepository _repository;

    public UpdateFileCommandHandler(ICodeSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateFileCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
            throw new NotFoundException($"Code session {request.SessionId} not found");

        session.UpdateFile(request.FileId, request.Content);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
