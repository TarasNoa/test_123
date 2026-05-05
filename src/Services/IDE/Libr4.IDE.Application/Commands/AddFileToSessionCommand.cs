namespace Libr4.IDE.Application.Commands;

using MediatR;
using Libr4.IDE.Domain;

public record AddFileToSessionCommand(
    Guid SessionId,
    string FileName,
    string Content,
    string Language
) : IRequest<Guid>;

public class AddFileToSessionCommandHandler : IRequestHandler<AddFileToSessionCommand, Guid>
{
    private readonly ICodeSessionRepository _repository;

    public AddFileToSessionCommandHandler(ICodeSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddFileToSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session == null)
            throw new NotFoundException($"Code session {request.SessionId} not found");

        session.AddFile(request.FileName, request.Content, request.Language);
        await _repository.SaveChangesAsync(cancellationToken);

        var file = session.Files.FirstOrDefault(f => f.FileName == request.FileName);
        return file?.Id ?? Guid.Empty;
    }
}
