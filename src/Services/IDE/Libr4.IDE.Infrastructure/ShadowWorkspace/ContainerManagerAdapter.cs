using Libr4.IDE.Infrastructure.Containers;
using AppSW = Libr4.IDE.Application.ShadowWorkspace;

namespace Libr4.IDE.Infrastructure.ShadowWorkspace;

/// <summary>
/// Adapter bridging Application.IContainerManager to Infrastructure.IContainerManager.
/// </summary>
public sealed class ContainerManagerAdapter : Application.ShadowWorkspace.IContainerManager
{
    private readonly Containers.IContainerManager _inner;

    public ContainerManagerAdapter(Containers.IContainerManager inner)
    {
        _inner = inner;
    }

    public async Task<string> CreateContainerAsync(string image, Dictionary<string, string>? env = null, CancellationToken ct = default)
    {
        var workspaceId = Guid.NewGuid().ToString("N");
        var info = await _inner.CreateContainerAsync(workspaceId, image);
        return info.Id;
    }

    public Task<bool> StartContainerAsync(string containerId, CancellationToken ct = default)
        => _inner.StartContainerAsync(containerId);

    public Task<bool> StopContainerAsync(string containerId, CancellationToken ct = default)
        => _inner.StopContainerAsync(containerId);

    public Task<bool> DeleteContainerAsync(string containerId, CancellationToken ct = default)
        => _inner.RemoveContainerAsync(containerId);

    public async Task<string> ExecuteCommandAsync(string containerId, string command, CancellationToken ct = default)
    {
        var success = await _inner.ExecuteCommandAsync(containerId, command);
        return success ? "ok" : "error";
    }

    public async Task<AppSW.ContainerStatus> GetStatusAsync(string containerId, CancellationToken ct = default)
    {
        var infos = await _inner.ListActiveAsync();
        var info = infos.FirstOrDefault(i => i.Id == containerId);
        if (info == null)
            return new AppSW.ContainerStatus { ContainerId = containerId, State = "NotFound" };

        return new AppSW.ContainerStatus
        {
            ContainerId = info.Id,
            State = info.Status,
            CreatedAt = info.CreatedAt
        };
    }
}
