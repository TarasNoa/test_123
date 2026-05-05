namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Interface for container manager
/// </summary>
public interface IContainerManager
{
    Task<string> CreateContainerAsync(string image, Dictionary<string, string>? env = null, CancellationToken ct = default);
    Task<bool> StartContainerAsync(string containerId, CancellationToken ct = default);
    Task<bool> StopContainerAsync(string containerId, CancellationToken ct = default);
    Task<bool> DeleteContainerAsync(string containerId, CancellationToken ct = default);
    Task<string> ExecuteCommandAsync(string containerId, string command, CancellationToken ct = default);
    Task<ContainerStatus> GetStatusAsync(string containerId, CancellationToken ct = default);
}

public class ContainerStatus
{
    public string ContainerId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty; // "Created", "Running", "Paused", "Stopped", "Deleted"
    public int? ExitCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
