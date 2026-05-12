using System.Threading.Tasks;

namespace Libr4.AI.Application.AgentExecution;

public enum ExecutionStatus { Pending, Running, Success, Failed, FixRequired, Fixed }

public record ExecutionResult
{
    public ExecutionStatus Status { get; init; }
    public string Output { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? StackTrace { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public int AttemptNumber { get; init; } = 1;
}

public interface ICodeExecutor
{
    Task<ExecutionResult> ExecuteAsync(string code, string language, int timeoutSeconds = 30);
}
