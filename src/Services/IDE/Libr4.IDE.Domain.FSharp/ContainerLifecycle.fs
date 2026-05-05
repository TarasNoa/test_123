// Golden Stack: F# Domain Logic (The Brain)
// Container Lifecycle State Machine with Discriminated Unions and Pattern Matching
// Pure functional approach - no side effects, all decisions computed

module Libr4.IDE.Domain.FSharp.ContainerLifecycle

open System

// ============================================
// Domain Types
// ============================================

/// Container unique identifier
type ContainerId = ContainerId of string

/// Environment types for different toolchains
type EnvironmentType =
    | DotNet
    | Python
    | JVM
    | Universal
    | Node

/// Container state transitions
/// Using Discriminated Unions for exhaustive pattern matching
type ContainerState =
    | PendingCreation
    | Creating of startTime: DateTime
    | Created of createdAt: DateTime
    | Starting of startedAt: DateTime
    | Running of startedAt: DateTime * healthCheck: HealthStatus
    | Stopping of stopRequestedAt: DateTime
    | Stopped of stoppedAt: DateTime * exitCode: int option
    | Failed of error: ContainerError * failedAt: DateTime
    | Deleting
    | Deleted of deletedAt: DateTime

/// Health status for running containers
and HealthStatus =
    | Healthy
    | Unhealthy of reason: string
    | Unknown

/// Container error types
and ContainerError =
    | ImagePullFailed of image: string * reason: string
    | CreateFailed of reason: string
    | StartFailed of reason: string
    | ResourceExhausted of resource: string
    | Timeout of operation: string * duration: TimeSpan
    | DockerDaemonError of message: string
    | NetworkError of message: string
    | VolumeMountFailed of path: string * reason: string

/// Resource allocation
type ResourceAllocation = {
    MemoryMB: int
    CpuCores: float
    MaxStorageGB: int option
}

/// Container definition
type ContainerDefinition = {
    Id: ContainerId
    Image: string
    Environment: EnvironmentType
    State: ContainerState
    Resources: ResourceAllocation
    CreatedAt: DateTime
    LastTransitionAt: DateTime
    Metadata: Map<string, string>
}

/// Lifecycle commands
/// All possible actions that can change container state
type LifecycleCommand =
    | Create of image: string * env: EnvironmentType * resources: ResourceAllocation
    | Start
    | Stop of timeout: TimeSpan
    | Delete of force: bool
    | ExecuteCommand of command: string * timeout: TimeSpan
    | UpdateResources of newResources: ResourceAllocation
    | HealthCheck
    | MarkForCleanup

/// Lifecycle events (results of commands)
type LifecycleEvent =
    | ContainerCreated of containerId: ContainerId * definition: ContainerDefinition
    | ContainerStarted of containerId: ContainerId * startedAt: DateTime
    | ContainerStopped of containerId: ContainerId * stoppedAt: DateTime * exitCode: int option
    | ContainerDeleted of containerId: ContainerId * deletedAt: DateTime
    | CommandExecuted of containerId: ContainerId * command: string * result: CommandResult
    | HealthStatusChanged of containerId: ContainerId * oldStatus: HealthStatus * newStatus: HealthStatus
    | ResourceUpdated of containerId: ContainerId * oldResources: ResourceAllocation * newResources: ResourceAllocation
    | ContainerFailed of containerId: ContainerId * error: ContainerError * failedAt: DateTime

/// Command execution result
and CommandResult =
    | Success of output: string * exitCode: int
    | Failure of error: string * exitCode: int
    | TimeoutExceeded of command: string * waitedFor: TimeSpan

/// Pool configuration
type PoolConfiguration = {
    Environment: EnvironmentType
    Image: string
    MinSize: int
    MaxSize: int
    MaxAge: TimeSpan
    PreWarmResources: ResourceAllocation
}

/// Pool state
type PoolState = {
    Config: PoolConfiguration
    AvailableContainers: ContainerDefinition list
    ActiveContainers: Map<ContainerId, ContainerDefinition>
    TotalCreated: int
    TotalReused: int
}

/// Resource pool for tracking system capacity
type SystemResourcePool = {
    TotalMemoryMB: int
    TotalCpuCores: float
    ReservedMemoryMB: int
    ReservedCpuCores: float
    ActiveContainers: int
    MaxContainers: int
}

// ============================================
// State Machine Functions
// ============================================

/// State transition function
/// Given current state and command, compute new state
/// Pure function - no side effects
let transition (current: ContainerDefinition) (command: LifecycleCommand) (at: DateTime) : Result<ContainerDefinition * LifecycleEvent, ContainerError> =
    match current.State, command with
    // Pending -> Creating
    | PendingCreation, Create(image, env, resources) ->
        let newDef = { current with State = Creating(at); Image = image; Environment = env; Resources = resources; LastTransitionAt = at }
        Ok(newDef, ContainerCreated(current.Id, newDef))
    
    // Creating -> Created (success) or Failed (error)
    | Creating _, Create _ ->
        Error(CreateFailed "Container already being created")
    
    | Creating startTime, _ when at - startTime > TimeSpan.FromMinutes(5) ->
        let error = Timeout("create", TimeSpan.FromMinutes(5))
        let failedDef = { current with State = Failed(error, at); LastTransitionAt = at }
        Ok(failedDef, ContainerFailed(current.Id, error, at))
    
    // Created -> Starting
    | Created _, Start ->
        let newDef = { current with State = Starting(at); LastTransitionAt = at }
        Ok(newDef, ContainerStarted(current.Id, at))  // Optimistic event
    
    | Created _, Stop _ ->
        Error(CreateFailed "Cannot stop a container that hasn't started")
    
    // Starting -> Running or Failed
    | Starting _, Start ->
        Error(StartFailed "Container already starting")
    
    | Starting startedAt, HealthCheck when at - startedAt > TimeSpan.FromSeconds(30) ->
        let newDef = { current with State = Running(at, Healthy); LastTransitionAt = at }
        Ok(newDef, HealthStatusChanged(current.Id, Unknown, Healthy))
    
    // Running -> Stopping
    | Running(startedAt, _), Stop timeout ->
        if at - startedAt < TimeSpan.FromSeconds(5) then
            Error(StartFailed "Container started too recently, minimum lifetime not met")
        else
            let newDef = { current with State = Stopping(at); LastTransitionAt = at }
            Ok(newDef, ContainerStopped(current.Id, at, None))
    
    | Running _, Delete false ->
        Error(CreateFailed "Cannot delete running container without force flag")
    
    // Stopping -> Stopped
    | Stopping stopRequested, _ when at - stopRequested > TimeSpan.FromMinutes(2) ->
        let error = Timeout("stop", at - stopRequested)
        let failedDef = { current with State = Failed(error, at); LastTransitionAt = at }
        Ok(failedDef, ContainerFailed(current.Id, error, at))
    
    | Stopping _, HealthCheck ->
        let stoppedDef = { current with State = Stopped(at, Some 0); LastTransitionAt = at }
        Ok(stoppedDef, ContainerStopped(current.Id, at, Some 0))
    
    // Stopped -> Deleting or Starting (restart)
    | Stopped _, Start ->
        let newDef = { current with State = Starting(at); LastTransitionAt = at }
        Ok(newDef, ContainerStarted(current.Id, at))
    
    | Stopped _, Delete force ->
        if not force then
            Error(CreateFailed "Cannot delete container without force flag")
        else
            let newDef = { current with State = Deleting; LastTransitionAt = at }
            Ok(newDef, ContainerDeleted(current.Id, at))
    
    // Failed -> Deleting
    | Failed _, Delete _ ->
        let newDef = { current with State = Deleting; LastTransitionAt = at }
        Ok(newDef, ContainerDeleted(current.Id, at))
    
    | Failed _, Start ->
        Error(StartFailed "Cannot restart failed container, must recreate")
    
    // Deleting -> Deleted
    | Deleting, _ ->
        let newDef = { current with State = Deleted(at); LastTransitionAt = at }
        Ok(newDef, ContainerDeleted(current.Id, at))
    
    | Deleted _, _ ->
        Error(CreateFailed "Container already deleted")
    
    // HealthCheck transitions
    | Running(startedAt, currentHealth), HealthCheck ->
        let runtime = at - startedAt
        let newHealth =
            if runtime > TimeSpan.FromHours(1) then
                Unhealthy "Container running too long, may have resource leaks"
            else
                Healthy
        
        if newHealth <> currentHealth then
            let newDef = { current with State = Running(at, newHealth); LastTransitionAt = at }
            Ok(newDef, HealthStatusChanged(current.Id, currentHealth, newHealth))
        else
            Ok(current, HealthStatusChanged(current.Id, currentHealth, currentHealth))  // No change
    
    // Update resources (only when stopped)
    | Stopped _, UpdateResources newResources ->
        let newDef = { current with Resources = newResources; LastTransitionAt = at }
        Ok(newDef, ResourceUpdated(current.Id, current.Resources, newResources))
    
    | _, UpdateResources _ ->
        Error(ResourceExhausted "Can only update resources when container is stopped")
    
    // Command execution
    | Running _, ExecuteCommand(cmd, timeout) ->
        // This would call the actual execution, but here we just return optimistic success
        let result = Success("Command accepted for execution", 0)
        Ok(current, CommandExecuted(current.Id, cmd, result))
    
    | _, ExecuteCommand _ ->
        Error(StartFailed "Can only execute commands in running containers")
    
    // Mark for cleanup (transitions to stopped)
    | Running _, MarkForCleanup
    | Starting _, MarkForCleanup ->
        let newDef = { current with State = Stopping(at); LastTransitionAt = at }
        Ok(newDef, ContainerStopped(current.Id, at, None))
    
    | state, cmd ->
        Error(CreateFailed $"Invalid transition from {state} with command {cmd}")

/// Check if a state is terminal (no further transitions possible)
let isTerminalState (state: ContainerState) : bool =
    match state with
    | Deleted _ -> true
    | _ -> false

/// Check if state can accept connections
let canAcceptCommands (state: ContainerState) : bool =
    match state with
    | Running _ -> true
    | _ -> false

/// Check if state is stable (not in transition)
let isStable (state: ContainerState) : bool =
    match state with
    | Creating _ | Starting _ | Stopping _ | Deleting -> false
    | _ -> true

/// Get current state name as string (for logging/monitoring)
let getStateName (state: ContainerState) : string =
    match state with
    | PendingCreation -> "PendingCreation"
    | Creating _ -> "Creating"
    | Created _ -> "Created"
    | Starting _ -> "Starting"
    | Running _ -> "Running"
    | Stopping _ -> "Stopping"
    | Stopped _ -> "Stopped"
    | Failed _ -> "Failed"
    | Deleting -> "Deleting"
    | Deleted _ -> "Deleted"

// ============================================
// Resource Allocation Logic
// ============================================

/// Check if system has resources for new container
let canAllocate (system: SystemResourcePool) (requested: ResourceAllocation) : bool =
    let availableMemory = system.TotalMemoryMB - system.ReservedMemoryMB
    let availableCpu = system.TotalCpuCores - system.ReservedCpuCores
    let canAddContainer = system.ActiveContainers < system.MaxContainers
    
    requested.MemoryMB <= availableMemory &&
    requested.CpuCores <= availableCpu &&
    canAddContainer

/// Allocate resources (returns updated pool)
let allocate (system: SystemResourcePool) (allocation: ResourceAllocation) : SystemResourcePool =
    { system with
        ReservedMemoryMB = system.ReservedMemoryMB + allocation.MemoryMB
        ReservedCpuCores = system.ReservedCpuCores + allocation.CpuCores
        ActiveContainers = system.ActiveContainers + 1
    }

/// Release resources (returns updated pool)
let release (system: SystemResourcePool) (allocation: ResourceAllocation) : SystemResourcePool =
    { system with
        ReservedMemoryMB = max 0 (system.ReservedMemoryMB - allocation.MemoryMB)
        ReservedCpuCores = max 0.0 (system.ReservedCpuCores - allocation.CpuCores)
        ActiveContainers = max 0 (system.ActiveContainers - 1)
    }

/// Get available resources
let getAvailableResources (system: SystemResourcePool) : ResourceAllocation = {
    MemoryMB = system.TotalMemoryMB - system.ReservedMemoryMB
    CpuCores = system.TotalCpuCores - system.ReservedCpuCores
    MaxStorageGB = None
}

/// Validate resource allocation
let validateResources (allocation: ResourceAllocation) : Result<ResourceAllocation, ContainerError> =
    if allocation.MemoryMB <= 0 then
        Error(ResourceExhausted "Memory allocation must be positive")
    elif allocation.CpuCores <= 0.0 then
        Error(ResourceExhausted "CPU allocation must be positive")
    elif allocation.MemoryMB > 1024 * 1024 then  // 1TB max
        Error(ResourceExhausted "Memory allocation exceeds maximum")
    elif allocation.CpuCores > 256.0 then  // 256 cores max
        Error(ResourceExhausted "CPU allocation exceeds maximum")
    else
        Ok allocation

// ============================================
// Pool Management Logic
// ============================================

/// Check if container is suitable for reuse (for pooling)
let isSuitableForReuse (container: ContainerDefinition) (maxAge: TimeSpan) (now: DateTime) : bool =
    match container.State with
    | Stopped _ ->
        let age = now - container.LastTransitionAt
        age < maxAge && age > TimeSpan.FromSeconds(10)  // Not too fresh, not too old
    | _ -> false

/// Calculate pool deficit (how many containers to warm up)
let calculatePoolDeficit (pool: PoolState) : int =
    let available = List.length pool.AvailableContainers
    max 0 (pool.Config.MinSize - available)

/// Calculate pool overflow (how many containers to remove)
let calculatePoolOverflow (pool: PoolState) : int =
    let available = List.length pool.AvailableContainers
    max 0 (available - pool.Config.MaxSize)

/// Identify stale containers for cleanup
let findStaleContainers (pool: PoolState) (now: DateTime) : ContainerDefinition list =
    pool.AvailableContainers
    |> List.filter (fun c -> not (isSuitableForReuse c pool.Config.MaxAge now))

/// Create default resource allocation for environment type
let defaultResources (env: EnvironmentType) : ResourceAllocation =
    match env with
    | DotNet -> { MemoryMB = 2048; CpuCores = 2.0; MaxStorageGB = Some 10 }
    | Python -> { MemoryMB = 1024; CpuCores = 1.0; MaxStorageGB = Some 5 }
    | JVM -> { MemoryMB = 4096; CpuCores = 2.0; MaxStorageGB = Some 10 }
    | Universal -> { MemoryMB = 8192; CpuCores = 4.0; MaxStorageGB = Some 20 }
    | Node -> { MemoryMB = 1024; CpuCores = 1.0; MaxStorageGB = Some 5 }

/// Get image for environment type
let environmentImage (env: EnvironmentType) : string =
    match env with
    | DotNet -> "libr4-env:dotnet"
    | Python -> "libr4-env:python"
    | JVM -> "libr4-env:jvm"
    | Universal -> "libr4-env:universal"
    | Node -> "libr4-env:node"

/// Create initial container definition
let createContainerDefinition (id: ContainerId) (image: string) (env: EnvironmentType) : ContainerDefinition = {
    Id = id
    Image = image
    Environment = env
    State = PendingCreation
    Resources = defaultResources env
    CreatedAt = DateTime.UtcNow
    LastTransitionAt = DateTime.UtcNow
    Metadata = Map.empty
}

/// Create pool configuration for environment
let createPoolConfig (env: EnvironmentType) : PoolConfiguration = {
    Environment = env
    Image = environmentImage env
    MinSize = 
        match env with
        | DotNet | Python -> 2
        | JVM | Universal | Node -> 1
    MaxSize = 
        match env with
        | DotNet | Python -> 5
        | JVM | Universal -> 3
        | Node -> 2
    MaxAge = TimeSpan.FromHours(1)
    PreWarmResources = defaultResources env
}

// ============================================
// Decision Functions
// ============================================

/// Decide whether to create new container or reuse from pool
type ContainerDecision =
    | CreateNew of definition: ContainerDefinition
    | ReuseFromPool of container: ContainerDefinition
    | WaitForPool
    | InsufficientResources

/// Make container acquisition decision
let decideContainerAcquisition 
    (pool: PoolState) 
    (system: SystemResourcePool) 
    (requestedEnv: EnvironmentType) 
    (now: DateTime) : ContainerDecision =
    
    // Check if pool has suitable container
    let suitable = 
        pool.AvailableContainers
        |> List.tryFind (fun c -> 
            c.Environment = requestedEnv && isSuitableForReuse c pool.Config.MaxAge now)
    
    match suitable with
    | Some container ->
        ReuseFromPool container
    | None ->
        // Check if we have resources
        let resources = defaultResources requestedEnv
        if canAllocate system resources then
            let id = ContainerId(Guid.NewGuid().ToString("N")[..12])
            let definition = createContainerDefinition id (environmentImage requestedEnv) requestedEnv
            CreateNew definition
        else
            InsufficientResources

/// Decide pool maintenance action
type MaintenanceDecision =
    | WarmUp of count: int * env: EnvironmentType
    | CleanUpStale of containers: ContainerDefinition list
    | NoAction

/// Make pool maintenance decision
let decidePoolMaintenance (pool: PoolState) (now: DateTime) : MaintenanceDecision =
    let stale = findStaleContainers pool now
    let deficit = calculatePoolDeficit pool
    let overflow = calculatePoolOverflow pool
    
    if not (List.isEmpty stale) then
        CleanUpStale stale
    elif deficit > 0 then
        WarmUp(deficit, pool.Config.Environment)
    elif overflow > 0 then
        // Remove oldest containers to get under max
        let toRemove = 
            pool.AvailableContainers
            |> List.sortBy (fun c -> c.LastTransitionAt)
            |> List.take overflow
        CleanUpStale toRemove
    else
        NoAction

// ============================================
// C# Interop Bridge
// ============================================

/// Helper for C# interop - convert container state to string
let containerStateToString (state: ContainerState) : string =
    getStateName state

/// Helper for C# interop - create container ID from string
let containerIdFromString (id: string) : ContainerId =
    ContainerId id

/// Helper for C# interop - get string from container ID
let stringFromContainerId (ContainerId id) : string =
    id

/// Helper for C# interop - create environment from string
let environmentFromString (env: string) : EnvironmentType =
    match env.ToLower() with
    | "dotnet" -> DotNet
    | "python" -> Python
    | "jvm" -> JVM
    | "universal" -> Universal
    | "node" -> Node
    | _ -> Universal

/// Helper for C# interop - get default command for environment
let defaultCommandForEnvironment (env: EnvironmentType) : string =
    match env with
    | DotNet -> "sleep infinity"
    | Python -> "sleep infinity"
    | JVM -> "sleep infinity"
    | Universal -> "sleep infinity"
    | Node -> "sleep infinity"
