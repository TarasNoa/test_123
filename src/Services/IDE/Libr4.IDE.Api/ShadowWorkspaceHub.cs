using Libr4.IDE.Application.ShadowWorkspace;
using Microsoft.AspNetCore.SignalR;

namespace Libr4.IDE.Api;

/// <summary>
/// SignalR Hub for real-time Shadow Workspace collaboration with containers, CRDT, and self-healing builds.
/// </summary>
public class ShadowWorkspaceHub : Hub
{
    private readonly ICrdtDocumentService _crdtService;
    private readonly IContainerManager _containerManager;
    private readonly IPreWarmedContainerPool _containerPool;
    private readonly IContainerLifecycleService _lifecycle;
    private readonly ISelfHealingBuildPipeline _buildPipeline;
    private readonly ILogger<ShadowWorkspaceHub> _logger;

    public ShadowWorkspaceHub(
        ICrdtDocumentService crdtService,
        IContainerManager containerManager,
        IPreWarmedContainerPool containerPool,
        IContainerLifecycleService lifecycle,
        ISelfHealingBuildPipeline buildPipeline,
        ILogger<ShadowWorkspaceHub> logger)
    {
        _crdtService = crdtService;
        _containerManager = containerManager;
        _containerPool = containerPool;
        _lifecycle = lifecycle;
        _buildPipeline = buildPipeline;
        _logger = logger;
    }

    /// <summary>
    /// Client joins a shadow workspace room
    /// </summary>
    public async Task JoinWorkspace(string workspaceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, workspaceId);

        var userId = Context.User?.Identity?.Name ?? "anonymous";
        _logger.LogInformation("Client {ConnectionId} (User: {UserId}) joined workspace {WorkspaceId}",
            Context.ConnectionId, userId, workspaceId);

        await Clients.Caller.SendAsync("Joined", new { WorkspaceId = workspaceId });
    }

    /// <summary>
    /// Client leaves workspace room
    /// </summary>
    public async Task LeaveWorkspace(string workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, workspaceId);
        _logger.LogInformation("Client {ConnectionId} left workspace {WorkspaceId}",
            Context.ConnectionId, workspaceId);
    }

    /// <summary>
    /// Called when connection disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Connection {ConnectionId} disconnected (Reason: {Reason})",
            Context.ConnectionId,
            exception?.Message ?? "Tab closed or network lost");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// CRDT document update from client
    /// </summary>
    public async Task DocumentUpdate(string workspaceId, string filePath, byte[] update, int sequence)
    {
        try
        {
            var documentId = $"{workspaceId}:{filePath}";

            await _crdtService.ApplyUpdateAsync(documentId, update);

            await Clients.OthersInGroup(workspaceId).SendAsync("DocumentUpdate", new
            {
                FilePath = filePath,
                Update = Convert.ToBase64String(update),
                Sequence = sequence,
                Author = Context.User?.Identity?.Name ?? "anonymous"
            });

            _logger.LogDebug("Document update broadcast for {FilePath} in {WorkspaceId}",
                filePath, workspaceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process document update");
            await Clients.Caller.SendAsync("Error", "Failed to apply document update");
        }
    }

    /// <summary>
    /// Create a new container for the workspace
    /// </summary>
    public async Task CreateContainer(string workspaceId, string image)
    {
        try
        {
            var containerId = await _containerManager.CreateContainerAsync(image);
            _logger.LogInformation("Created container {ContainerId} for workspace {WorkspaceId}", containerId, workspaceId);
            await Clients.Caller.SendAsync("ContainerCreated", new { ContainerId = containerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create container for workspace {WorkspaceId}", workspaceId);
            await Clients.Caller.SendAsync("Error", "Failed to create container");
        }
    }

    /// <summary>
    /// Start a container
    /// </summary>
    public async Task StartContainer(string containerId)
    {
        var result = await _containerManager.StartContainerAsync(containerId);
        await Clients.Caller.SendAsync("ContainerStarted", new { ContainerId = containerId, Success = result });
    }

    /// <summary>
    /// Stop a container
    /// </summary>
    public async Task StopContainer(string containerId)
    {
        var result = await _containerManager.StopContainerAsync(containerId);
        await Clients.Caller.SendAsync("ContainerStopped", new { ContainerId = containerId, Success = result });
    }

    /// <summary>
    /// Execute a command inside a container
    /// </summary>
    public async Task ExecuteCommand(string containerId, string command)
    {
        try
        {
            var output = await _containerManager.ExecuteCommandAsync(containerId, command);
            await Clients.Caller.SendAsync("CommandOutput", new { ContainerId = containerId, Output = output });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed on {ContainerId}", containerId);
            await Clients.Caller.SendAsync("Error", $"Command failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get container status
    /// </summary>
    public async Task GetContainerStatus(string containerId)
    {
        var status = await _containerManager.GetStatusAsync(containerId);
        await Clients.Caller.SendAsync("ContainerStatus", status);
    }

    /// <summary>
    /// Acquire a pre-warmed container from the pool
    /// </summary>
    public async Task AcquirePrewarmedContainer()
    {
        try
        {
            var containerId = await _containerPool.AcquireContainerAsync();
            await Clients.Caller.SendAsync("PrewarmedContainerAcquired", new { ContainerId = containerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire pre-warmed container");
            await Clients.Caller.SendAsync("Error", "Pool exhausted");
        }
    }

    /// <summary>
    /// Start build in workspace
    /// </summary>
    public async Task StartBuild(string workspaceId, string projectPath)
    {
        _logger.LogInformation("Starting build for workspace {WorkspaceId}", workspaceId);

        await Clients.Group(workspaceId).SendAsync("BuildStarted", new { WorkspaceId = workspaceId, StartedAt = DateTime.UtcNow });

        try
        {
            var result = await _buildPipeline.BuildAsync(projectPath);
            await Clients.Group(workspaceId).SendAsync("BuildCompleted", new
            {
                Success = result.Success,
                Duration = result.Duration,
                ErrorCount = result.Errors.Length,
                RetryCount = result.RetryCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build failed for workspace {WorkspaceId}", workspaceId);
            await Clients.Group(workspaceId).SendAsync("BuildFailed", new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Start self-healing build with AI fixes
    /// </summary>
    public async Task StartSelfHealingBuild(string workspaceId, string projectPath, int maxIterations = 3)
    {
        _logger.LogInformation("Starting self-healing build for {WorkspaceId}", workspaceId);

        await Clients.Group(workspaceId).SendAsync("SelfHealingBuildStarted", new { MaxIterations = maxIterations });

        for (int i = 0; i < maxIterations; i++)
        {
            await Clients.Group(workspaceId).SendAsync("BuildAttempt", new { Iteration = i + 1 });

            var result = await _buildPipeline.BuildAsync(projectPath, new BuildOptions { MaxRetries = 1 });

            if (result.Success)
            {
                await Clients.Group(workspaceId).SendAsync("BuildCompleted", new { Success = true, Iteration = i + 1, Duration = result.Duration });
                return;
            }

            if (i < maxIterations - 1 && result.Errors.Length > 0)
            {
                var fixedAny = false;
                foreach (var err in result.Errors)
                {
                    if (await _buildPipeline.DiagnoseAndFixAsync(projectPath, err))
                        fixedAny = true;
                }
                if (!fixedAny) break;
            }
        }

        await Clients.Group(workspaceId).SendAsync("BuildFailed", new { Message = $"Failed after {maxIterations} attempts" });
    }
}
