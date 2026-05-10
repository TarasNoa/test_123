using System;
using System.Collections.Generic;

namespace Libr4.IDE.Domain.AgentExecution;

public enum ExecutionStatus
{
    Pending,
    Running,
    Success,
    Failed,
    FixRequired,
    Fixed
}

public record ExecutionResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ExecutionContextId { get; init; }
    public ExecutionStatus Status { get; init; }
    public string Output { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public int AttemptNumber { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record CodeGeneration
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Language { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public class AgentExecutionContext
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Task { get; set; } = string.Empty;
    public List<CodeGeneration> CodeGenerations { get; set; } = new();
    public List<ExecutionResult> ExecutionResults { get; set; } = new();
    public ExecutionStatus CurrentStatus { get; set; } = ExecutionStatus.Pending;
    public int MaxRetryAttempts { get; set; } = 3;
    public int CurrentAttempt { get; set; } = 0;
    public string? LastErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public void AddCodeGeneration(string language, string code, string? description = null)
    {
        CodeGenerations.Add(new CodeGeneration
        {
            Language = language,
            Code = code,
            Description = description
        });
        CurrentStatus = ExecutionStatus.Running;
    }

    public void AddExecutionResult(ExecutionResult result)
    {
        ExecutionResults.Add(result);
        CurrentAttempt++;
        CurrentStatus = result.Status;

        if (result.Status == ExecutionStatus.Failed)
        {
            LastErrorMessage = result.ErrorMessage;
        }
    }

    public bool CanRetry => CurrentAttempt < MaxRetryAttempts && CurrentStatus == ExecutionStatus.FixRequired;
}