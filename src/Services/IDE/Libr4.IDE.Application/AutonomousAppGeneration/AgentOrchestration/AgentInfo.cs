namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentOrchestration;

public class AgentInfo
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AgentStatus Status { get; init; }
    public List<AgentInfo> SubAgents { get; init; } = new();
    public string? Purpose { get; init; }
    public string? Input { get; init; }
    public string? Output { get; init; }

    public AgentInfo(
        string name,
        string role,
        AgentStatus status = AgentStatus.Idle,
        string? purpose = null,
        string? input = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Role = role;
        Status = status;
        Purpose = purpose;
        Input = input;
    }

    public AgentInfo AddSubAgent(AgentInfo subAgent)
    {
        SubAgents.Add(subAgent);
        return this;
    }

    public AgentInfo WithStatus(AgentStatus status)
    {
        return new AgentInfo(Name, Role, status, Purpose, Input)
        {
            Id = Id,
            Description = Description,
            SubAgents = SubAgents,
            Output = Output
        };
    }

    public AgentInfo WithOutput(string output)
    {
        return new AgentInfo(Name, Role, Status, Purpose, Input)
        {
            Id = Id,
            Description = Description,
            SubAgents = SubAgents,
            Output = output
        };
    }
}
