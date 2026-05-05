namespace Libr4.IDE.Application.Commands;

using MediatR;
using Libr4.IDE.Domain;
using Libr4.Shared.Kernel;

public record CreateCodeSessionCommand(
    string Title,
    string Description,
    string Language,
    string ProjectId,
    Guid CreatorId
) : IRequest<Guid>;

public class CreateCodeSessionCommandHandler : IRequestHandler<CreateCodeSessionCommand, Guid>
{
    private readonly ICodeSessionRepository _repository;

    public CreateCodeSessionCommandHandler(ICodeSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateCodeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = new CodeSession(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Language,
            request.ProjectId,
            request.CreatorId
        );

        await _repository.AddAsync(session, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return session.Id;
    }
}
