/*
using Libr4.IDE.Application.AgentMemorySystem.Commands;
using Libr4.IDE.Application.AgentMemorySystem.DTOs;
using Libr4.IDE.Domain.AgentMemorySystem;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AgentMemorySystem.Handlers;

public class CreateMemoryCommandHandler : IRequestHandler<CreateMemoryCommand, MemoryDto>
{
    private readonly IMemoryRepository _memoryRepository;
    private readonly ILogger<CreateMemoryCommandHandler> _logger;

    public CreateMemoryCommandHandler(
        IMemoryRepository memoryRepository,
        ILogger<CreateMemoryCommandHandler> logger)
    {
        _memoryRepository = memoryRepository;
        _logger = logger;
    }

    public async Task<MemoryDto> Handle(CreateMemoryCommand request, CancellationToken ct)
    {
        var memory = Memory.Create(
            request.Content,
            request.Type,
            request.Tags,
            request.AgentId);

        await _memoryRepository.SaveAsync(memory, ct);

        _logger.LogInformation("Created memory {MemoryId} for agent {AgentId}", memory.Id, request.AgentId);

        return new MemoryDto
        {
            Id = memory.Id,
            Content = memory.Content,
            Type = memory.Type,
            Tags = memory.Tags,
            AgentId = memory.AgentId,
            CreatedAt = memory.CreatedAt
        };
    }
}
*/
