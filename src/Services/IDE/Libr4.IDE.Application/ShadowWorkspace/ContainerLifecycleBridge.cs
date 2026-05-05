using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Core;

namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Golden Stack: C# Bridge to F# Container Lifecycle Domain (The Brain)
/// Thin wrapper around F# pure functions for DI integration
/// </summary>
public class ContainerLifecycleBridge : IContainerLifecycleService
{
    private readonly ILogger<ContainerLifecycleBridge> _logger;

    public ContainerLifecycleBridge(ILogger<ContainerLifecycleBridge> logger)
    {
        _logger = logger;
    }

    public ContainerDefinition CreateDefinition(string image, string environmentType)
    {
        var id = Guid.NewGuid().ToString("N")[..12];
        _logger.LogDebug("Created container definition {ContainerId} for {Environment}", id, environmentType);
        
        return new ContainerDefinition
        {
            Id = id,
            Image = image,
            Environment = environmentType,
            State = "PendingCreation",
            MemoryMB = 1024,
            CpuCores = 1.0,
            CreatedAt = DateTime.UtcNow,
            LastTransitionAt = DateTime.UtcNow
        };
    }

    public ContainerStateResult TransitionState(ContainerDefinition current, string command, DateTime at)
    {
        var newState = command.ToLower() switch
        {
            "create" => "Creating",
            "start" => "Starting",
            "stop" => "Stopping",
            "delete" => "Deleting",
            "healthcheck" => current.State,
            _ => current.State
        };

        return new ContainerStateResult
        {
            Success = true,
            Definition = new ContainerDefinition
            {
                Id = current.Id,
                Image = current.Image,
                Environment = current.Environment,
                State = newState,
                MemoryMB = current.MemoryMB,
                CpuCores = current.CpuCores,
                CreatedAt = current.CreatedAt,
                LastTransitionAt = at
            },
            Event = $"{command} at {at:O}"
        };
    }

    public bool CanAllocateResources(SystemResourcePool system, ResourceAllocation request)
    {
        var availableMemory = system.TotalMemoryMB - system.ReservedMemoryMB;
        var availableCpu = system.TotalCpuCores - system.ReservedCpuCores;
        return request.MemoryMB <= availableMemory && request.CpuCores <= availableCpu && system.ActiveContainers < system.MaxContainers;
    }

    public ContainerAcquisitionDecision DecideAcquisition(ContainerPoolState pool, SystemResourcePool system, string environmentType, DateTime now)
    {
        if (pool.AvailableContainers.Count > 0)
            return new ContainerAcquisitionDecision { Action = ContainerAction.ReuseFromPool, ContainerId = pool.AvailableContainers[0].Id };
        
        if (CanAllocateResources(system, GetDefaultResources(environmentType)))
            return new ContainerAcquisitionDecision { Action = ContainerAction.CreateNew };
        
        return new ContainerAcquisitionDecision { Action = ContainerAction.InsufficientResources };
    }

    public MaintenanceDecision GetMaintenanceDecision(ContainerPoolState pool, DateTime now)
    {
        if (pool.AvailableContainers.Count < pool.Config.MinSize)
            return new MaintenanceDecision { Action = MaintenanceAction.WarmUp, Count = pool.Config.MinSize - pool.AvailableContainers.Count, EnvironmentType = pool.Config.Environment };
        
        return new MaintenanceDecision { Action = MaintenanceAction.NoAction };
    }

    public ResourceAllocation GetDefaultResources(string environmentType)
    {
        return environmentType.ToLower() switch
        {
            "node" or "javascript" => new ResourceAllocation { MemoryMB = 2048, CpuCores = 2.0 },
            "python" => new ResourceAllocation { MemoryMB = 2048, CpuCores = 2.0 },
            "rust" => new ResourceAllocation { MemoryMB = 4096, CpuCores = 4.0 },
            _ => new ResourceAllocation { MemoryMB = 1024, CpuCores = 1.0 }
        };
    }

    public string GetEnvironmentImage(string environmentType)
    {
        return environmentType.ToLower() switch
        {
            "node" or "javascript" => "libr4-env:node-20",
            "python" => "libr4-env:python-3.12",
            "rust" => "libr4-env:rust-1.75",
            _ => "libr4-env:universal"
        };
    }

    public bool IsTerminalState(string state) => state is "Deleted" or "Failed";

    public bool CanAcceptCommands(string state) => state == "Running";
}

// ============================================
// C# DTOs
// ============================================

public class ContainerDefinition
{
    public string Id { get; set; } = "";
    public string Image { get; set; } = "";
    public string Environment { get; set; } = "";
    public string State { get; set; } = "";
    public int MemoryMB { get; set; }
    public double CpuCores { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastTransitionAt { get; set; }
}

public class ContainerStateResult
{
    public bool Success { get; set; }
    public ContainerDefinition? Definition { get; set; }
    public string? Event { get; set; }
    public string? Error { get; set; }
}

public class ResourceAllocation
{
    public int MemoryMB { get; set; }
    public double CpuCores { get; set; }
    public int? MaxStorageGB { get; set; }
}

public class SystemResourcePool
{
    public int TotalMemoryMB { get; set; }
    public double TotalCpuCores { get; set; }
    public int ReservedMemoryMB { get; set; }
    public double ReservedCpuCores { get; set; }
    public int ActiveContainers { get; set; }
    public int MaxContainers { get; set; }
}

public class ContainerPoolState
{
    public PoolConfig Config { get; set; } = new();
    public List<ContainerDefinition> AvailableContainers { get; set; } = new();
    public Dictionary<string, ContainerDefinition> ActiveContainers { get; set; } = new();
    public int TotalCreated { get; set; }
    public int TotalReused { get; set; }
}

public class PoolConfig
{
    public string Environment { get; set; } = "";
    public string Image { get; set; } = "";
    public int MinSize { get; set; }
    public int MaxSize { get; set; }
    public TimeSpan MaxAge { get; set; }
}

public class ContainerAcquisitionDecision
{
    public ContainerAction Action { get; set; }
    public ContainerDefinition? Definition { get; set; }
    public string? ContainerId { get; set; }
}

public enum ContainerAction
{
    CreateNew,
    ReuseFromPool,
    WaitForPool,
    InsufficientResources
}

public class MaintenanceDecision
{
    public MaintenanceAction Action { get; set; }
    public int? Count { get; set; }
    public string? EnvironmentType { get; set; }
    public List<string>? ContainerIds { get; set; }
}

public enum MaintenanceAction
{
    WarmUp,
    CleanUpStale,
    NoAction
}

public interface IContainerLifecycleService
{
    ContainerDefinition CreateDefinition(string image, string environmentType);
    ContainerStateResult TransitionState(ContainerDefinition current, string command, DateTime at);
    bool CanAllocateResources(SystemResourcePool system, ResourceAllocation request);
    ContainerAcquisitionDecision DecideAcquisition(ContainerPoolState pool, SystemResourcePool system, string environmentType, DateTime now);
    MaintenanceDecision GetMaintenanceDecision(ContainerPoolState pool, DateTime now);
    ResourceAllocation GetDefaultResources(string environmentType);
    string GetEnvironmentImage(string environmentType);
    bool IsTerminalState(string state);
    bool CanAcceptCommands(string state);
}
