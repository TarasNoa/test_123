using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.CodespaceAI;

public enum CodespaceStatus { Creating, Running, Stopped, Error, Deleting }
public enum MachineType { Basic2Core, Standard4Core, Premium8Core, Enterprise16Core }

public class Codespace
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CodespaceStatus Status { get; set; } = CodespaceStatus.Creating;
    public MachineType Machine { get; set; } = MachineType.Basic2Core;
    public string Region { get; set; } = "us-east-1";
    public string? ContainerImage { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
    public string? ConnectionUrl { get; set; }
    public int RunningTimeSeconds { get; set; }
    public decimal CostPerHour { get; set; }
    public decimal TotalCost { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? StoppedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Start(DateTimeOffset now) { Status = CodespaceStatus.Running; StartedAt = now; }
    public void Stop(DateTimeOffset now)
    {
        Status = CodespaceStatus.Stopped;
        StoppedAt = now;
        if (StartedAt.HasValue)
        {
            RunningTimeSeconds += (int)(now - StartedAt.Value).TotalSeconds;
            TotalCost += CostPerHour * (decimal)(now - StartedAt.Value).TotalHours;
        }
    }
}
