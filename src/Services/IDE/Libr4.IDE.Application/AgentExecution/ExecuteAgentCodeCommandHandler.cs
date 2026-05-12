using Libr4.Shared.Infrastructure.Messaging;
using Libr4.Shared.Infrastructure.Events;
using Libr4.Shared.Kernel.Domain;
using Libr4.IDE.Domain.AgentExecution;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AgentExecution;

public class ExecuteAgentCodeCommand
{
    public Guid AgentId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
}

public class ExecuteAgentCodeCommandHandler : ICommandHandler<ExecuteAgentCodeCommand, AgentExecutionContext>
{
    private readonly ICodeExecutor _executor;
    private readonly ICodeErrorAnalyzer _errorAnalyzer;
    private readonly ICodeRepairService _repairService;
    private readonly IAgentExecutionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ExecuteAgentCodeCommandHandler> _logger;

    public ExecuteAgentCodeCommandHandler(
        ICodeExecutor executor,
        ICodeErrorAnalyzer errorAnalyzer,
        ICodeRepairService repairService,
        IAgentExecutionRepository repository,
        IEventPublisher eventPublisher,
        ILogger<ExecuteAgentCodeCommandHandler> logger)
    {
        _executor = executor;
        _errorAnalyzer = errorAnalyzer;
        _repairService = repairService;
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<AgentExecutionContext> Handle(ExecuteAgentCodeCommand command)
    {
        var context = new AgentExecutionContext
        {
            AgentId = command.AgentId,
            WorkspaceId = command.WorkspaceId,
            Task = command.Task,
            StartedAt = DateTime.UtcNow
        };

        context.AddCodeGeneration(command.Language, command.Code, command.Task);
        await _repository.AddAsync(context);

        var result = await ExecuteWithRetryAsync(command.Code, command.Language, context);

        context.AddExecutionResult(result);
        context.CompletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(context);
        await _eventPublisher.PublishAsync(new AgentCodeExecutedEvent(context.Id, result.Status));

        _logger.LogInformation($"Code execution completed: {result.Status}");

        return context;
    }

    private async Task<ExecutionResult> ExecuteWithRetryAsync(string code, string language, AgentExecutionContext context)
    {
        string currentCode = code;
        ExecutionResult result = null!;

        while (context.CanRetry || context.CurrentAttempt == 0)
        {
            result = await _executor.ExecuteAsync(currentCode, language);

            if (result.Status == ExecutionStatus.Success)
            {
                _logger.LogInformation($"Code executed successfully on attempt {context.CurrentAttempt + 1}");
                return result;
            }

            if (result.Status == ExecutionStatus.FixRequired && context.CanRetry)
            {
                _logger.LogWarning($"Execution failed, attempting auto-fix... Attempt {context.CurrentAttempt + 1}/{context.MaxRetryAttempts}");

                var (success, repairedCode) = await _repairService.AttemptAutoFixAsync(currentCode, result.ErrorMessage ?? "", language);

                if (success)
                {
                    currentCode = repairedCode;
                    context.AddCodeGeneration(language, repairedCode, $"Auto-fixed version (attempt {context.CurrentAttempt + 1})");
                    continue;
                }
                else
                {
                    _logger.LogError("Auto-fix failed, returning error result");
                    return result;
                }
            }

            break;
        }

        return result;
    }
}

public record AgentCodeExecutedEvent(Guid ExecutionContextId, ExecutionStatus Status) : DomainEvent;