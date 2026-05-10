using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libr4.AI.Domain.Agents.AgentHierarchy;

public interface IAgent
{
    Guid Id { get; }
    string Name { get; }
    AgentType Type { get; }
    Guid? ParentAgentId { get; }
    List<Guid> ChildAgentIds { get; }
    
    Task<AgentResponse> ExecuteAsync(AgentRequest request);
    Task<bool> CanHandleAsync(string taskType);
    Task RegisterChildAgentAsync(IAgent childAgent);
    Task UnregisterChildAgentAsync(Guid childAgentId);
    AgentCapabilities GetCapabilities();
}

public enum AgentType
{
    Orchestrator,      // Главный координатор
    Specialist,        // Специалист по конкретной области
    Executor,          // Исполнитель задач
    Analyzer,          // Анализатор данных
    Planner,           // Планировщик
    CodeWriter,        // Генератор кода
    CodeReviewer,      // Рецензент кода
    Debugger,          // Отладчик
    DocumentationBot,  // Документирование
    TaskScheduler      // Планировка задач
}

public record AgentRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Task { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
    public int Priority { get; init; } = 5; // 1-10
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record AgentResponse
{
    public Guid RequestId { get; init; }
    public Guid AgentId { get; init; }
    public bool Success { get; init; }
    public string Result { get; init; } = string.Empty;
    public string? Error { get; init; }
    public List<AgentResponse> SubAgentResponses { get; init; } = new();
    public TimeSpan ExecutionTime { get; init; }
    public int Confidence { get; init; } // 0-100
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

public record AgentCapabilities
{
    public List<string> SupportedTasks { get; init; } = new();
    public List<string> SupportedLanguages { get; init; } = new();
    public int MaxConcurrentTasks { get; init; }
    public TimeSpan AverageExecutionTime { get; init; }
    public double SuccessRate { get; init; } // 0.0 - 1.0
}