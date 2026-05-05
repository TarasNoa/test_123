using FluentValidation;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Domain.Agents;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.AI.Application.Agents.Commands;

public record CreateAgentCommand(
    string Name,
    string Description,
    AgentType Type,
    string Model,
    string SystemPrompt) : IRequest<Result<Guid>>;

public class CreateAgentValidator : AbstractValidator<CreateAgentCommand>
{
    public CreateAgentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SystemPrompt).NotEmpty();
    }
}

public class CreateAgentHandler : IRequestHandler<CreateAgentCommand, Result<Guid>>
{
    private readonly IAIDbContext _context;

    public CreateAgentHandler(IAIDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAgentCommand request, CancellationToken cancellationToken)
    {
        var agent = new Agent(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            request.Type,
            request.SystemPrompt,
            request.Model);

        // Add default tools based on type
        switch (request.Type)
        {
            case AgentType.Code:
                agent.AddTool(new AgentTool(Guid.NewGuid(), "file_read", "Read file contents"));
                agent.AddTool(new AgentTool(Guid.NewGuid(), "file_write", "Write or modify files"));
                agent.AddTool(new AgentTool(Guid.NewGuid(), "search_files", "Search files with glob pattern"));
                break;
            case AgentType.Researcher:
                agent.AddTool(new AgentTool(Guid.NewGuid(), "web_search", "Search the web"));
                agent.AddTool(new AgentTool(Guid.NewGuid(), "fetch_url", "Fetch content from URL"));
                break;
        }

        await _context.Agents.AddAsync(agent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(agent.Id);
    }
}
