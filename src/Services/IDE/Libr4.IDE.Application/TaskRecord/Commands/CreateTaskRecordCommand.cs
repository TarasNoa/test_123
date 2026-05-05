using MediatR;
using Libr4.IDE.Application.TaskRecord.DTOs;

namespace Libr4.IDE.Application.TaskRecord.Commands;

/// <summary>
/// Command to create a task record
/// </summary>
public record CreateTaskRecordCommand : IRequest<TaskRecordDto>
{
    public string TaskId { get; init; } = string.Empty;
    public string InitialState { get; init; } = "{}";
}
