/*
using Libr4.IDE.Application.SeniorRolePrompts.Commands;
using Libr4.IDE.Application.SeniorRolePrompts.DTOs;
using Libr4.IDE.Domain.SeniorRolePrompts;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.SeniorRolePrompts.Handlers;

public class GenerateRolePromptCommandHandler : IRequestHandler<GenerateRolePromptCommand, RolePromptDto>
{
    private readonly IRolePromptRepository _rolePromptRepository;
    private readonly ILogger<GenerateRolePromptCommandHandler> _logger;

    public GenerateRolePromptCommandHandler(
        IRolePromptRepository rolePromptRepository,
        ILogger<GenerateRolePromptCommandHandler> logger)
    {
        _rolePromptRepository = rolePromptRepository;
        _logger = logger;
    }

    public async Task<RolePromptDto> Handle(GenerateRolePromptCommand request, CancellationToken ct)
    {
        var rolePrompt = RolePrompt.Generate(
            request.Role,
            request.Context,
            request.Requirements);

        await _rolePromptRepository.SaveAsync(rolePrompt, ct);

        _logger.LogInformation("Generated role prompt {PromptId} for role {Role}", rolePrompt.Id, request.Role);

        return new RolePromptDto
        {
            Id = rolePrompt.Id,
            Role = rolePrompt.Role,
            Prompt = rolePrompt.Prompt,
            Context = rolePrompt.Context,
            Requirements = rolePrompt.Requirements,
            CreatedAt = rolePrompt.CreatedAt
        };
    }
}
*/
