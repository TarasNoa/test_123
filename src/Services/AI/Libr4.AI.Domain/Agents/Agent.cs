using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.AI.Domain.Agents.Events;

namespace Libr4.AI.Domain.Agents;

public enum AgentType
{
    Chat,        // General conversation
    Code,        // Code generation/review
    Planner,     // Task planning
    Reviewer,    // Code review
    Researcher   // Research assistant
}

public enum AgentStatus
{
    Idle,
    Working,
    Paused,
    Error
}

public class Agent : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AgentType Type { get; private set; }
    public string SystemPrompt { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public AgentStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<AgentTool> _allowedTools = new();
    public IReadOnlyCollection<AgentTool> AllowedTools => _allowedTools.AsReadOnly();

    private Agent() { } // EF Core

    public Agent(
        Guid id,
        string name,
        string description,
        AgentType type,
        string systemPrompt,
        string model) : base(id)
    {
        Name = name;
        Description = description;
        Type = type;
        SystemPrompt = systemPrompt;
        Model = model;
        Status = AgentStatus.Idle;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        RaiseDomainEvent(new AgentCreatedEvent(id, name, type, model, CreatedAt));
    }

    public void AddTool(AgentTool tool)
    {
        if (!_allowedTools.Any(t => t.Name == tool.Name))
        {
            _allowedTools.Add(tool);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new AgentToolAddedEvent(Id, tool.Name, UpdatedAt));
        }
    }

    public void RemoveTool(string toolName)
    {
        var tool = _allowedTools.FirstOrDefault(t => t.Name == toolName);
        if (tool != null)
        {
            _allowedTools.Remove(tool);
            UpdatedAt = DateTime.UtcNow;
            RaiseDomainEvent(new AgentToolRemovedEvent(Id, toolName, UpdatedAt));
        }
    }

    public void SetStatus(AgentStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AgentStatusChangedEvent(Id, status, UpdatedAt));
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = AgentStatus.Idle;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AgentDeactivatedEvent(Id, UpdatedAt));
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AgentActivatedEvent(Id, UpdatedAt));
    }

    public void UpdatePrompt(string prompt)
    {
        SystemPrompt = prompt;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AgentPromptUpdatedEvent(Id, UpdatedAt));
    }
}

public class AgentTool : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Parameters { get; private set; } // JSON schema

    private AgentTool() { } // EF Core

    public AgentTool(Guid id, string name, string description, string? parameters = null) : base(id)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}

// Pre-defined agents
public static class PredefinedAgents
{
    public static Agent CreateCodeAgent(string model = "gpt-4")
    {
        var agent = new Agent(
            Guid.NewGuid(),
            "Code Assistant",
            "Expert programmer for code generation, review, and debugging",
            AgentType.Code,
            "You are an expert programmer. Write clean, efficient, well-documented code. " +
            "Always follow best practices and explain your reasoning.",
            model);

        agent.AddTool(new AgentTool(Guid.NewGuid(), "file_read", "Read file contents"));
        agent.AddTool(new AgentTool(Guid.NewGuid(), "file_write", "Write file contents"));
        agent.AddTool(new AgentTool(Guid.NewGuid(), "search_files", "Search files with pattern"));

        return agent;
    }

    public static Agent CreatePlannerAgent(string model = "gpt-4")
    {
        return new Agent(
            Guid.NewGuid(),
            "Task Planner",
            "Breaks down complex tasks into actionable steps",
            AgentType.Planner,
            "You are a planning assistant. Break down complex tasks into clear, " +
            "actionable steps. Provide estimated time and dependencies for each step.",
            model);
    }

    public static Agent CreateReviewerAgent(string model = "gpt-4")
    {
        return new Agent(
            Guid.NewGuid(),
            "Code Reviewer",
            "Reviews code for quality, security, and best practices",
            AgentType.Reviewer,
            "You are a code reviewer. Analyze code for: bugs, security issues, " +
            "performance problems, style violations, and maintainability. Be thorough but constructive.",
            model);
    }
}
