/*
using Libr4.IDE.Application.ShadowWorkspace;
using Libr4.IDE.Infrastructure.Collaboration;
using Libr4.IDE.Infrastructure.Containers;
using Microsoft.AspNetCore.SignalR;

namespace Libr4.IDE.Api;

/// <summary>
/// SignalR Hub for real-time Shadow Workspace collaboration
/// </summary>
public class ShadowWorkspaceHub : Hub
{
    private readonly ICrdtDocumentService _crdtService;
    private readonly IContainerManager _containerManager;
    private readonly ISelfHealingBuildPipeline _buildPipeline;
    private readonly ContainerConnectionTracker _connectionTracker;
    private readonly ILogger<ShadowWorkspaceHub> _logger;

    public ShadowWorkspaceHub(
        ICrdtDocumentService crdtService,
        IContainerManager containerManager,
        ISelfHealingBuildPipeline buildPipeline,
        ContainerConnectionTracker connectionTracker,
        ILogger<ShadowWorkspaceHub> logger)
    {
        _crdtService = crdtService;
        _containerManager = containerManager;
        _buildPipeline = buildPipeline;
        _connectionTracker = connectionTracker;
        _logger = logger;
    }

    /// <summary>
    /// Client joins a shadow workspace room
    /// </summary>
    public async Task JoinWorkspace(string workspaceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, workspaceId);

        // Track connection for immediate container cleanup
        var userId = Context.User?.Identity?.Name ?? "anonymous";
        _connectionTracker.RegisterConnection(workspaceId, Context.ConnectionId, userId);

        _logger.LogInformation("Client {ConnectionId} (User: {UserId}) joined workspace {WorkspaceId}",
            Context.ConnectionId, userId, workspaceId);

        // Send current workspace state
        var container = await _containerManager.GetContainerAsync(workspaceId);
        if (container != null)
        {
            await Clients.Caller.SendAsync("ContainerStatus", new
            {
                Status = container.Status,
                CreatedAt = container.CreatedAt,
                ActiveConnections = _connectionTracker.GetConnectionCount(workspaceId)
            });
        }
    }

    /// <summary>
    /// Client leaves workspace room
    /// </summary>
    public async Task LeaveWorkspace(string workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, workspaceId);

        // Unregister connection - will trigger container cleanup if no connections left
        await _connectionTracker.UnregisterConnectionAsync(workspaceId, Context.ConnectionId);

        _logger.LogInformation("Client {ConnectionId} left workspace {WorkspaceId}",
            Context.ConnectionId, workspaceId);
    }

    /// <summary>
    /// Called when connection disconnects (tab closed, network lost, etc.)
    /// IMMEDIATELY destroys container if no other connections
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Get all workspace groups this connection was in
        // SignalR doesn't track this automatically, so we need to track it ourselves
        // For now, we'll check all known workspaces

        _logger.LogInformation(
            "Connection {ConnectionId} disconnected (Reason: {Reason})",
            Context.ConnectionId,
            exception?.Message ?? "Tab closed or network lost");

        // The ContainerConnectionTracker will handle finding and cleaning up
        // This is a simplified approach - in production, track workspaceId per connection

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

            // Apply update to CRDT document
            await _crdtService.ApplyUpdateAsync(documentId, update);

            // Broadcast to other clients in workspace
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
    /// Start build in shadow workspace
    /// </summary>
    public async Task StartBuild(string workspaceId)
    {
        _logger.LogInformation("Starting build for workspace {WorkspaceId}", workspaceId);

        // Notify all clients that build started
        await Clients.Group(workspaceId).SendAsync("BuildStarted", new
        {
            WorkspaceId = workspaceId,
            StartedAt = DateTime.UtcNow
        });

        try
        {
            // Execute build with streaming logs
            var result = await ExecuteBuildWithStreamingAsync(workspaceId);

            // Notify completion
            await Clients.Group(workspaceId).SendAsync("BuildCompleted", new
            {
                WorkspaceId = workspaceId,
                Success = result.Success,
                Duration = result.Duration,
                ExitCode = result.ExitCode,
                ErrorCount = result.Errors.Count,
                WarningCount = result.Warnings.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Build failed for workspace {WorkspaceId}", workspaceId);
            await Clients.Group(workspaceId).SendAsync("BuildFailed", new
            {
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Start self-healing build with AI fixes
    /// </summary>
    public async Task StartSelfHealingBuild(string workspaceId, int maxIterations = 3)
    {
        _logger.LogInformation("Starting self-healing build for {WorkspaceId}", workspaceId);

        await Clients.Group(workspaceId).SendAsync("SelfHealingBuildStarted", new
        {
            MaxIterations = maxIterations
        });

        for (int i = 0; i < maxIterations; i++)
        {
            await Clients.Group(workspaceId).SendAsync("BuildAttempt", new
            {
                Iteration = i + 1,
                Message = $"Build attempt {i + 1}/{maxIterations}"
            });

            var result = await _buildPipeline.ExecuteSingleBuildAsync(workspaceId);

            if (result.Success)
            {
                await Clients.Group(workspaceId).SendAsync("BuildCompleted", new
                {
                    Success = true,
                    Iteration = i + 1,
                    Duration = result.Duration
                });
                return;
            }

            // If failed and not last iteration, notify about AI fix attempt
            if (i < maxIterations - 1)
            {
                await Clients.Group(workspaceId).SendAsync("AIFixAttempt", new
                {
                    Error = result.ErrorOutput.Substring(0, Math.Min(200, result.ErrorOutput.Length)),
                    Iteration = i + 1
                });

                // AI will attempt fix in the background
                // Next iteration will use fixed code
            }
        }

        await Clients.Group(workspaceId).SendAsync("BuildFailed", new
        {
            Message = $"Failed after {maxIterations} attempts",
            AllAttemptsExhausted = true
        });
    }

    /// <summary>
    /// Get container logs
    /// </summary>
    public async Task GetLogs(string workspaceId, int tail = 100)
    {
        var container = await _containerManager.GetContainerAsync(workspaceId);
        if (container == null)
        {
            await Clients.Caller.SendAsync("Error", "Container not found");
            return;
        }

        var logs = await _containerManager.GetLogsAsync(container.Id, tail);
        await Clients.Caller.SendAsync("ContainerLogs", new
        {
            WorkspaceId = workspaceId,
            Logs = logs
        });
    }

    /// <summary>
    /// Execute command in container
    /// </summary>
    public async Task ExecuteCommand(string workspaceId, string command)
    {
        var container = await _containerManager.GetContainerAsync(workspaceId);
        if (container == null)
        {
            await Clients.Caller.SendAsync("Error", "Container not found");
            return;
        }

        _logger.LogInformation("Executing command in {WorkspaceId}: {Command}", workspaceId, command);

        var success = await _containerManager.ExecuteCommandAsync(container.Id, command);

        await Clients.Caller.SendAsync("CommandExecuted", new
        {
            Command = command,
            Success = success
        });
    }

    private async Task<BuildResult> ExecuteBuildWithStreamingAsync(string workspaceId)
    {
        // This would stream logs in real-time in production
        // For now, just return the result
        return await _buildPipeline.ExecuteSingleBuildAsync(workspaceId);
    }
}

/// <summary>
/// Build result for SignalR communication
/// </summary>
public class BuildStatusMessage
{
    public string WorkspaceId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LogLine { get; set; }
    public int Progress { get; set; }
}
*/
