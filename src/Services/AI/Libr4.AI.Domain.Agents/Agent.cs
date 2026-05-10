using System;
using Libr4.Shared.Kernel.Domain;
using Libr4.AI.Domain.Agents.Events;

namespace Libr4.AI.Domain.Agents;

public class Agent : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Prompt { get; private set; } = string.Empty;
    public AgentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Agent() { }

    public static Agent Create(string name, string role, string prompt)
    {
        var agent = new Agent
        {
            Id = Guid.NewGuid(),
            Name = name,
            Role = role,
            Prompt = prompt,
            Status = AgentStatus.Idle,
            CreatedAt = DateTimeOffset.UtcNow
        };

        agent.RaiseDomainEvent(new AgentCreatedEvent(agent.Id, name, role, agent.CreatedAt));
        return agent;
    }

    public void Activate()
    {
        if (Status == AgentStatus.Active) return;

        Status = AgentStatus.Active;
        RaiseDomainEvent(new AgentActivatedEvent(Id, DateTimeOffset.UtcNow));
    }

    public void Deactivate()
    {
        if (Status == AgentStatus.Inactive) return;

        Status = AgentStatus.Inactive;
        RaiseDomainEvent(new AgentDeactivatedEvent(Id, DateTimeOffset.UtcNow));
    }

    public void UpdatePrompt(string newPrompt)
    {
        if (Prompt == newPrompt) return;

        Prompt = newPrompt;
        RaiseDomainEvent(new AgentPromptUpdatedEvent(Id, newPrompt, DateTimeOffset.UtcNow));
    }
}

public enum AgentStatus
{
    Idle,
    Active,
    Inactive
}

// Добавить event handlers
public class AgentCreatedEventHandler : INotificationHandler<AgentCreatedEvent>
{
    public async Task Handle(AgentCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Отправить email или лог
        await Task.CompletedTask;
    }
}