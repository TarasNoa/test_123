/*
using Libr4.IDE.Application.TaskRecord.Commands;
using Libr4.IDE.Application.TaskRecord.DTOs;
using Libr4.IDE.Domain.TaskRecord;
using Libr4.IDE.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.TaskRecord.Handlers;

public class CreateTaskRecordCommandHandler : IRequestHandler<CreateTaskRecordCommand, TaskRecordDto>
{
    private readonly ITaskRecordRepository _taskRecordRepository;
    private readonly ILogger<CreateTaskRecordCommandHandler> _logger;

    public CreateTaskRecordCommandHandler(
        ITaskRecordRepository taskRecordRepository,
        ILogger<CreateTaskRecordCommandHandler> logger)
    {
        _taskRecordRepository = taskRecordRepository;
        _logger = logger;
    }

    public async Task<TaskRecordDto> Handle(CreateTaskRecordCommand request, CancellationToken ct)
    {
        var taskRecord = TaskRecord.Create(
            request.Title,
            request.Description,
            request.Status,
            request.Priority,
            request.AgentId);

        await _taskRecordRepository.SaveAsync(taskRecord, ct);

        _logger.LogInformation("Created task record {TaskRecordId} for agent {AgentId}", taskRecord.Id, request.AgentId);

        return new TaskRecordDto
        {
            Id = taskRecord.Id,
            Title = taskRecord.Title,
            Description = taskRecord.Description,
            Status = taskRecord.Status,
            Priority = taskRecord.Priority,
            AgentId = taskRecord.AgentId,
            CreatedAt = taskRecord.CreatedAt
        };
    }
}
*/
