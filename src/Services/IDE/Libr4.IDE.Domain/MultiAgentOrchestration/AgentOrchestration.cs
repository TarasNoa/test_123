using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.MultiAgentOrchestration.Events;

namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Swarm topology (from Ruflo)
/// </summary>
public enum SwarmTopology
{
    /// <summary>
    /// Queen-led hierarchy with central coordinator
    /// </summary>
    Hierarchical,
    
    /// <summary>
    /// Mesh network with peer-to-peer communication
    /// </summary>
    Mesh,
    
    /// <summary>
    /// Adaptive topology that changes based on task complexity
    /// </summary>
    Adaptive,
    
    /// <summary>
    /// Hive mind with shared consciousness
    /// </summary>
    HiveMind
}

/// <summary>
/// Consensus mechanism (from Ruflo)
/// </summary>
public enum ConsensusMechanism
{
    /// <summary>
    /// Simple majority voting
    /// </summary>
    Majority,
    
    /// <summary>
    /// Weighted voting based on agent performance
    /// </summary>
    Weighted,
    
    /// <summary>
    /// Full consensus required
    /// </summary>
    Unanimous,
    
    /// <summary>
    /// Raft consensus algorithm
    /// </summary>
    Raft,
    
    /// <summary>
    /// Byzantine fault tolerance
    /// </summary>
    Byzantine,
    
    /// <summary>
    /// Gossip protocol
    /// </summary>
    Gossip
}

/// <summary>
/// Learning pattern (from Ruflo SONA)
/// </summary>
public class LearningPattern
{
    public Guid Id { get; private set; }
    public string PatternName { get; private set; }
    public string Description { get; private set; }
    public double SuccessRate { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public Dictionary<string, object> PatternData { get; private set; }
    
    public LearningPattern(string patternName, string description)
    {
        Id = Guid.NewGuid();
        PatternName = patternName;
        Description = description;
        SuccessRate = 0.5;
        UsageCount = 0;
        LastUsedAt = DateTime.UtcNow;
        PatternData = new Dictionary<string, object>();
    }
    
    public void RecordSuccess(bool success)
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        
        // Update success rate with exponential moving average
        SuccessRate = (SuccessRate * 0.9) + (success ? 0.1 : 0.0);
    }
    
    public void UpdatePatternData(string key, object value)
    {
        PatternData[key] = value;
    }
}

/// <summary>
/// Delegation mode for subagent spawning (from hermes-agent)
/// </summary>
public enum DelegationMode
{
    /// <summary>
    /// Sequential delegation
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Parallel delegation for independent tasks
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Competitive delegation - multiple agents compete
    /// </summary>
    Competitive
}

/// <summary>
/// Skill creation request (from hermes-agent autonomous skill creation)
/// </summary>
public class SkillCreationRequest
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string ProcedureCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    
    public SkillCreationRequest(string name, string description, string procedureCode)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        ProcedureCode = procedureCode;
        CreatedAt = DateTime.UtcNow;
        IsApproved = false;
    }
    
    public void Approve()
    {
        IsApproved = true;
        ApprovedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// RPC tool call for subagent (from hermes-agent)
/// </summary>
public class RPCToolCall
{
    public Guid Id { get; private set; }
    public string ToolName { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    public Guid? CallingAgentId { get; private set; }
    public DateTime CalledAt { get; private set; }
    public object? Result { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    public RPCToolCall(string toolName, Dictionary<string, object> parameters, Guid? callingAgentId = null)
    {
        Id = Guid.NewGuid();
        ToolName = toolName;
        Parameters = parameters;
        CallingAgentId = callingAgentId;
        CalledAt = DateTime.UtcNow;
    }
    
    public void Complete(object result)
    {
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// AggregateRoot for multi-agent orchestration
/// Enhanced with hermes-agent concepts (delegation, skill creation, RPC tools)
/// </summary>
public class AgentOrchestration : AggregateRoot<Guid>
{
    public string OrchestrationId { get; private set; }
    public List<AgentInstance> Agents { get; private set; }
    public OrchestrationTask MainTask { get; private set; }
    public List<AgentCommunication> Communications { get; private set; }
    public string Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    /// <summary>
    /// Delegation mode for this orchestration (from hermes-agent)
    /// </summary>
    public DelegationMode DelegationMode { get; private set; }
    
    /// <summary>
    /// Skill creation requests (from hermes-agent autonomous skill creation)
    /// </summary>
    public List<SkillCreationRequest> SkillCreationRequests { get; private set; }
    
    /// <summary>
    /// RPC tool calls for subagent coordination (from hermes-agent)
    /// </summary>
    public List<RPCToolCall> RPCToolCalls { get; private set; }
    
    /// <summary>
    /// Cross-session recall context (from hermes-agent FTS5 search)
    /// </summary>
    public List<string> RecallContext { get; private set; }
    
    /// <summary>
    /// Swarm topology (from Ruflo)
    /// </summary>
    public SwarmTopology Topology { get; private set; }
    
    /// <summary>
    /// Consensus mechanism (from Ruflo)
    /// </summary>
    public ConsensusMechanism ConsensusMechanism { get; private set; }
    
    /// <summary>
    /// Learning patterns (from Ruflo SONA)
    /// </summary>
    public List<LearningPattern> LearningPatterns { get; private set; }
    
    /// <summary>
    /// Vector memory index (from Ruflo AgentDB)
    /// </summary>
    public string? VectorMemoryIndex { get; private set; }
    
    private AgentOrchestration() { }
    
    public AgentOrchestration(
        string orchestrationId,
        OrchestrationTask mainTask,
        List<AgentInstance>? agents = null,
        DelegationMode delegationMode = DelegationMode.Sequential,
        SwarmTopology topology = SwarmTopology.Hierarchical,
        ConsensusMechanism consensusMechanism = ConsensusMechanism.Raft)
    {
        Id = Guid.NewGuid();
        OrchestrationId = orchestrationId;
        MainTask = mainTask;
        Agents = agents ?? new List<AgentInstance>();
        Communications = new List<AgentCommunication>();
        Status = "initializing";
        StartedAt = DateTime.UtcNow;
        CompletedAt = null;
        DelegationMode = delegationMode;
        SkillCreationRequests = new List<SkillCreationRequest>();
        RPCToolCalls = new List<RPCToolCall>();
        RecallContext = new List<string>();
        Topology = topology;
        ConsensusMechanism = consensusMechanism;
        LearningPatterns = new List<LearningPattern>();
    }
    
    public void AddAgent(AgentInstance agent)
    {
        if (agent != null)
        {
            Agents.Add(agent);
        }
    }
    
    public void AddCommunication(AgentCommunication communication)
    {
        if (communication != null)
        {
            Communications.Add(communication);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    public AgentInstance? GetAgentById(Guid agentId)
    {
        return Agents.FirstOrDefault(a => a.Id == agentId);
    }
    
    public void UpdateAgentStatus(Guid agentId, AgentStatus status)
    {
        var agent = GetAgentById(agentId);
        if (agent != null)
        {
            agent.SetStatus(status);
        }
    }
    
    /// <summary>
    /// Spawn isolated subagent for delegation (from hermes-agent)
    /// </summary>
    public AgentInstance SpawnSubagent(string agentType, string taskDescription)
    {
        var subagent = new AgentInstance(
            agentType,
            $"Subagent for: {taskDescription}",
            AgentStatus.Idle);
        AddAgent(subagent);
        return subagent;
    }
    
    /// <summary>
    /// Request autonomous skill creation (from hermes-agent)
    /// </summary>
    public SkillCreationRequest RequestSkillCreation(string name, string description, string procedureCode)
    {
        var request = new SkillCreationRequest(name, description, procedureCode);
        SkillCreationRequests.Add(request);
        return request;
    }
    
    /// <summary>
    /// Execute RPC tool call (from hermes-agent)
    /// </summary>
    public RPCToolCall ExecuteRPCToolCall(string toolName, Dictionary<string, object> parameters, Guid? callingAgentId = null)
    {
        var call = new RPCToolCall(toolName, parameters, callingAgentId);
        RPCToolCalls.Add(call);
        return call;
    }
    
    /// <summary>
    /// Add recall context from cross-session search (from hermes-agent)
    /// </summary>
    public void AddRecallContext(string context)
    {
        RecallContext.Add(context);
    }
    
    /// <summary>
    /// Record a learning pattern (from Ruflo SONA)
    /// </summary>
    public LearningPattern RecordLearningPattern(string patternName, string description, bool success)
    {
        var pattern = LearningPatterns.FirstOrDefault(p => p.PatternName == patternName);
        if (pattern == null)
        {
            pattern = new LearningPattern(patternName, description);
            LearningPatterns.Add(pattern);
        }
        pattern.RecordSuccess(success);
        return pattern;
    }
    
    /// <summary>
    /// Achieve consensus using configured mechanism (from Ruflo)
    /// </summary>
    public Dictionary<string, object> AchieveConsensus(Dictionary<Guid, object> agentResults)
    {
        switch (ConsensusMechanism)
        {
            case ConsensusMechanism.Majority:
                return MajorityConsensus(agentResults);
            case ConsensusMechanism.Raft:
                return RaftConsensus(agentResults);
            case ConsensusMechanism.Byzantine:
                return ByzantineConsensus(agentResults);
            case ConsensusMechanism.Gossip:
                return GossipConsensus(agentResults);
            default:
                return MajorityConsensus(agentResults);
        }
    }
    
    private Dictionary<string, object> MajorityConsensus(Dictionary<Guid, object> agentResults)
    {
        // Simple majority vote implementation
        // In real implementation, this would aggregate results and pick the majority
        return new Dictionary<string, object>
        {
            ["consensus"] = "majority",
            ["participants"] = agentResults.Count,
            ["result"] = agentResults.FirstOrDefault().Value
        };
    }
    
    private Dictionary<string, object> RaftConsensus(Dictionary<Guid, object> agentResults)
    {
        // Raft consensus with leader election
        // In real implementation, this would follow Raft protocol
        return new Dictionary<string, object>
        {
            ["consensus"] = "raft",
            ["leader"] = Agents.FirstOrDefault()?.Id,
            ["result"] = agentResults.FirstOrDefault().Value
        };
    }
    
    private Dictionary<string, object> ByzantineConsensus(Dictionary<Guid, object> agentResults)
    {
        // Byzantine fault tolerance
        // In real implementation, this would handle up to f faulty nodes
        return new Dictionary<string, object>
        {
            ["consensus"] = "byzantine",
            ["tolerance"] = (agentResults.Count - 1) / 3,
            ["result"] = agentResults.FirstOrDefault().Value
        };
    }
    
    private Dictionary<string, object> GossipConsensus(Dictionary<Guid, object> agentResults)
    {
        // Gossip protocol for information propagation
        // In real implementation, this would use peer-to-peer gossip
        return new Dictionary<string, object>
        {
            ["consensus"] = "gossip",
            ["rounds"] = Math.Ceiling(Math.Log(agentResults.Count)),
            ["result"] = agentResults.FirstOrDefault().Value
        };
    }
    
    /// <summary>
    /// Set vector memory index (from Ruflo AgentDB)
    /// </summary>
    public void SetVectorMemoryIndex(string indexName)
    {
        VectorMemoryIndex = indexName;
    }
    
    /// <summary>
    /// Marks the orchestration as started and raises a domain event
    /// </summary>
    public void MarkAsStarted()
    {
        AddDomainEvent(new AgentOrchestrationStartedEvent(Id, OrchestrationId));
    }
    
    /// <summary>
    /// Marks an agent assignment and raises a domain event
    /// </summary>
    public void MarkAgentAssigned(Guid agentId, Guid taskId)
    {
        AddDomainEvent(new AgentAssignedEvent(Id, OrchestrationId, agentId, taskId));
    }
    
    /// <summary>
    /// Marks agent communication and raises a domain event
    /// </summary>
    public void MarkAgentCommunication(AgentCommunication communication)
    {
        AddDomainEvent(new AgentCommunicationEvent(Id, OrchestrationId, communication.FromAgentId, communication.ToAgentId));
    }
    
    public static AgentOrchestration Create(
        string orchestrationId,
        OrchestrationTask mainTask,
        List<AgentInstance>? agents = null,
        DelegationMode delegationMode = DelegationMode.Sequential,
        SwarmTopology topology = SwarmTopology.Hierarchical,
        ConsensusMechanism consensusMechanism = ConsensusMechanism.Raft)
    {
        return new AgentOrchestration(orchestrationId, mainTask, agents, delegationMode, topology, consensusMechanism);
    }
}

/// <summary>
/// Sub-agent task (from deer-flow)
/// </summary>
public class SubAgentTask
{
    public Guid Id { get; private set; }
    public Guid ParentAgentId { get; private set; }
    public string Description { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Dictionary<string, object> Result { get; private set; }
    public List<string> AssignedTools { get; private set; }
    
    public SubAgentTask(Guid parentAgentId, string description)
    {
        Id = Guid.NewGuid();
        ParentAgentId = parentAgentId;
        Description = description;
        Status = TaskStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        Result = new Dictionary<string, object>();
        AssignedTools = new List<string>();
    }
    
    public void Complete(Dictionary<string, object> result)
    {
        Status = TaskStatus.Completed;
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Task status (from deer-flow)
/// </summary>
public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Sandbox environment (from deer-flow)
/// </summary>
public class SandboxEnvironment
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public Dictionary<string, string> FileSystem { get; private set; }
    public bool ShellExecutionEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public SandboxEnvironment(string name, string type)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        FileSystem = new Dictionary<string, string>();
        ShellExecutionEnabled = false;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddFile(string path, string content)
    {
        FileSystem[path] = content;
    }
    
    public void EnableShellExecution()
    {
        ShellExecutionEnabled = true;
    }
}

/// <summary>
/// Task dependency (from claude-task-master)
/// </summary>
public class TaskDependency
{
    public Guid Id { get; private set; }
    public Guid DependentTaskId { get; private set; }
    public Guid DependsOnTaskId { get; private set; }
    public DependencyType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public TaskDependency(Guid dependentTaskId, Guid dependsOnTaskId, DependencyType type)
    {
        Id = Guid.NewGuid();
        DependentTaskId = dependentTaskId;
        DependsOnTaskId = dependsOnTaskId;
        Type = type;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Dependency type (from claude-task-master)
/// </summary>
public enum DependencyType
{
    Required,
    Optional,
    Recommended
}

/// <summary>
/// Task workstream (from claude-task-master)
/// </summary>
public class TaskWorkstream
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<Guid> TaskIds { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    public TaskWorkstream(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        TaskIds = new List<Guid>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddTask(Guid taskId)
    {
        TaskIds.Add(taskId);
    }
    
    public void MarkCompleted()
    {
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Task tag (from claude-task-master)
/// </summary>
public class TaskTag
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Color { get; private set; }
    public List<Guid> TaskIds { get; private set; }
    
    public TaskTag(string name, string color)
    {
        Id = Guid.NewGuid();
        Name = name;
        Color = color;
        TaskIds = new List<Guid>();
        // Relationships = new Dictionary<Guid, Relationship>();
    }
    
    public void AddTask(Guid taskId)
    {
        if (!TaskIds.Contains(taskId))
        {
            TaskIds.Add(taskId);
        }
    }
    
    /*
    public void InvalidateRelationship(Guid relationshipId)
    {
        if (Relationships.ContainsKey(relationshipId))
        {
            Relationships[relationshipId].Invalidate();
        }
    }
    */
}

/// <summary>
/// Provider profile (from openclaude)
/// </summary>
public class ProviderProfile
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string ProviderType { get; private set; } // openai, anthropic, gemini, ollama, etc.
    public string BaseUrl { get; private set; }
    public string ApiKey { get; private set; }
    public string DefaultModel { get; private set; }
    public Dictionary<string, string> AdditionalConfig { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUsed { get; private set; }
    
    public ProviderProfile(string name, string providerType, string baseUrl, string apiKey, string defaultModel)
    {
        Id = Guid.NewGuid();
        Name = name;
        ProviderType = providerType;
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        DefaultModel = defaultModel;
        AdditionalConfig = new Dictionary<string, string>();
        CreatedAt = DateTime.UtcNow;
        LastUsed = DateTime.UtcNow;
    }
    
    public void UpdateLastUsed()
    {
        LastUsed = DateTime.UtcNow;
    }
    
    public void SetConfig(string key, string value)
    {
        AdditionalConfig[key] = value;
    }
}

/// <summary>
/// Agent routing configuration (from openclaude)
/// </summary>
public class AgentRoutingConfig
{
    public Dictionary<string, string> AgentToProviderMap { get; private set; }
    public string DefaultProvider { get; private set; }
    
    public AgentRoutingConfig(string defaultProvider)
    {
        AgentToProviderMap = new Dictionary<string, string>();
        DefaultProvider = defaultProvider;
    }
    
    public void AddRoute(string agentType, string providerProfileId)
    {
        AgentToProviderMap[agentType] = providerProfileId;
    }
    
    public string GetProviderForAgent(string agentType)
    {
        return AgentToProviderMap.TryGetValue(agentType, out var providerId) ? providerId : DefaultProvider;
    }
}

/// <summary>
/// Agent type specialization (from opencode)
/// </summary>
public enum AgentSpecializationType
{
    /// <summary>
    /// Build agent - full-access for development work
    /// </summary>
    Build,
    
    /// <summary>
    /// Plan agent - read-only for analysis and exploration
    /// </summary>
    Plan,
    
    /// <summary>
    /// General agent - complex searches and multistep tasks
    /// </summary>
    General,
    
    /// <summary>
    /// Code reviewer - quality and security review
    /// </summary>
    CodeReviewer,
    
    /// <summary>
    /// Security reviewer - vulnerability analysis
    /// </summary>
    SecurityReviewer,
    
    /// <summary>
    /// Architect - system design decisions
    /// </summary>
    Architect
}

/// <summary>
/// Instinct (from everything-claude-code continuous learning v2)
/// </summary>
public class Instinct
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Action { get; private set; }
    public string Evidence { get; private set; }
    public List<string> Examples { get; private set; }
    public double ConfidenceScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUsed { get; private set; }
    public int UsageCount { get; private set; }
    public bool IsApproved { get; private set; }
    
    public Instinct(string name, string description, string action, string evidence)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Action = action;
        Evidence = evidence;
        Examples = new List<string>();
        ConfidenceScore = 0.5;
        CreatedAt = DateTime.UtcNow;
        LastUsed = DateTime.UtcNow;
        UsageCount = 0;
        IsApproved = false;
    }
    
    public void AddExample(string example)
    {
        Examples.Add(example);
    }
    
    public void RecordUsage(bool success)
    {
        UsageCount++;
        LastUsed = DateTime.UtcNow;
        
        // Update confidence score with exponential moving average
        ConfidenceScore = (ConfidenceScore * 0.9) + (success ? 0.1 : 0.0);
    }
    
    public void Approve()
    {
        IsApproved = true;
    }
}

/// <summary>
/// Verification loop (from everything-claude-code)
/// </summary>
public class VerificationLoop
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<VerificationStep> Steps { get; private set; }
    public VerificationStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Dictionary<string, object> Results { get; private set; }
    
    public VerificationLoop(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Steps = new List<VerificationStep>();
        Status = VerificationStatus.Pending;
        StartedAt = DateTime.UtcNow;
        Results = new Dictionary<string, object>();
    }
    
    public void AddStep(VerificationStep step)
    {
        Steps.Add(step);
    }
    
    public void Start()
    {
        Status = VerificationStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(Dictionary<string, object> results)
    {
        Status = VerificationStatus.Completed;
        Results = results;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void Fail()
    {
        Status = VerificationStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Verification step (from everything-claude-code)
/// </summary>
public class VerificationStep
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public VerificationStepStatus Status { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public object? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    public VerificationStep(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Status = VerificationStepStatus.Pending;
    }
    
    public void Start()
    {
        Status = VerificationStepStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }
    
    public void Complete(object result)
    {
        Status = VerificationStepStatus.Completed;
        Result = result;
        CompletedAt = DateTime.UtcNow;
    }
    
    public void Fail(string errorMessage)
    {
        Status = VerificationStepStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Verification status (from everything-claude-code)
/// </summary>
public enum VerificationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Verification step status (from everything-claude-code)
/// </summary>
public enum VerificationStepStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Safety check (from pentest-copilot)
/// </summary>
public class SafetyCheck
{
    public Guid Id { get; private set; }
    public string Command { get; private set; }
    public string Reason { get; private set; }
    public SafetyLevel Level { get; private set; }
    public bool RequiresExplicitApproval { get; private set; }
    
    public SafetyCheck(string command, string reason, SafetyLevel level)
    {
        Id = Guid.NewGuid();
        Command = command;
        Reason = reason;
        Level = level;
        RequiresExplicitApproval = level == SafetyLevel.Critical || level == SafetyLevel.High;
    }
}

/// <summary>
/// Safety level (from pentest-copilot)
/// </summary>
public enum SafetyLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Tool capability (from pentest-copilot)
/// </summary>
public class ToolCapability
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public string Description { get; private set; }
    public List<string> Dependencies { get; private set; }
    public bool IsInstalled { get; private set; }
    
    public ToolCapability(string name, string category, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Category = category;
        Description = description;
        Dependencies = new List<string>();
        IsInstalled = false;
    }
    
    public void AddDependency(string dependency)
    {
        Dependencies.Add(dependency);
    }
    
    public void MarkInstalled()
    {
        IsInstalled = true;
    }
}

/// <summary>
/// Capability registry (from pentest-copilot)
/// </summary>
public class CapabilityRegistry
{
    public Dictionary<string, List<ToolCapability>> CapabilitiesByCategory { get; private set; }
    
    public CapabilityRegistry()
    {
        CapabilitiesByCategory = new Dictionary<string, List<ToolCapability>>();
        InitializeDefaultCapabilities();
    }
    
    private void InitializeDefaultCapabilities()
    {
        // Network capabilities
        CapabilitiesByCategory["network"] = new List<ToolCapability>
        {
            new ToolCapability("nmap", "network", "Network scanning and discovery"),
            new ToolCapability("netcat", "network", "Network utility for reading/writing network connections"),
            new ToolCapability("curl", "network", "Data transfer tool")
        };
        
        // Recon capabilities
        CapabilitiesByCategory["recon"] = new List<ToolCapability>
        {
            new ToolCapability("sublist3r", "recon", "Subdomain enumeration"),
            new ToolCapability("amass", "recon", "Attack surface discovery")
        };
        
        // Core capabilities
        CapabilitiesByCategory["core"] = new List<ToolCapability>
        {
            new ToolCapability("python3", "core", "Python interpreter"),
            new ToolCapability("bash", "core", "Shell command execution")
        };
    }
    
    public void RegisterCapability(ToolCapability capability)
    {
        if (!CapabilitiesByCategory.ContainsKey(capability.Category))
        {
            CapabilitiesByCategory[capability.Category] = new List<ToolCapability>();
        }
        CapabilitiesByCategory[capability.Category].Add(capability);
    }
    
    public List<ToolCapability> GetCapabilitiesByCategory(string category)
    {
        return CapabilitiesByCategory.TryGetValue(category, out var capabilities) ? capabilities : new List<ToolCapability>();
    }
}

/// <summary>
/// Adversarial agent role (from bug-hunter)
/// </summary>
public enum AdversarialAgentRole
{
    Hunter,
    Skeptic,
    Referee
}

/// <summary>
/// Adversarial debate (from bug-hunter)
/// </summary>
public class AdversarialDebate
{
    public Guid Id { get; private set; }
    public string Subject { get; private set; }
    public List<AgentArgument> Arguments { get; private set; }
    public DebateVerdict Verdict { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    public AdversarialDebate(string subject)
    {
        Id = Guid.NewGuid();
        Subject = subject;
        Arguments = new List<AgentArgument>();
        Verdict = DebateVerdict.Pending;
        StartedAt = DateTime.UtcNow;
    }
    
    public void AddArgument(AgentArgument argument)
    {
        Arguments.Add(argument);
    }
    
    public void SetVerdict(DebateVerdict verdict)
    {
        Verdict = verdict;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Agent argument (from bug-hunter)
/// </summary>
public class AgentArgument
{
    public Guid Id { get; private set; }
    public AdversarialAgentRole Role { get; private set; }
    public string Content { get; private set; }
    public List<string> Evidence { get; private set; }
    public DateTime Timestamp { get; private set; }
    
    public AgentArgument(AdversarialAgentRole role, string content)
    {
        Id = Guid.NewGuid();
        Role = role;
        Content = content;
        Evidence = new List<string>();
        Timestamp = DateTime.UtcNow;
    }
    
    public void AddEvidence(string evidence)
    {
        Evidence.Add(evidence);
    }
}

/// <summary>
/// Debate verdict (from bug-hunter)
/// </summary>
public enum DebateVerdict
{
    Pending,
    Confirmed,
    Dismissed,
    Inconclusive
}

/// <summary>
/// STRIDE threat category (from bug-hunter)
/// </summary>
public enum StrideThreatCategory
{
    Spoofing,
    Tampering,
    Repudiation,
    InformationDisclosure,
    DenialOfService,
    ElevationOfPrivilege
}

/// <summary>
/// Security finding (from bug-hunter)
/// </summary>
public class SecurityFinding
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public Severity Severity { get; private set; }
    public StrideThreatCategory StrideCategory { get; private set; }
    public string CweIdentifier { get; private set; }
    public double CvssScore { get; private set; }
    public string CvssVector { get; private set; }
    public string FilePath { get; private set; }
    public int StartLine { get; private set; }
    public int EndLine { get; private set; }
    public Reachability Reachability { get; private set; }
    public Exploitability Exploitability { get; private set; }
    public ProofOfConcept ProofOfConcept { get; private set; }
    public double ConfidenceScore { get; private set; }
    public DateTime DiscoveredAt { get; private set; }
    
    public SecurityFinding(string title, string description, Severity severity)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Severity = severity;
        ConfidenceScore = 0.0;
        DiscoveredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Reachability (from bug-hunter)
/// </summary>
public enum Reachability
{
    External,
    Authenticated,
    Internal,
    Unreachable
}

/// <summary>
/// Exploitability (from bug-hunter)
/// </summary>
public enum Exploitability
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// Proof of concept (from bug-hunter)
/// </summary>
public class ProofOfConcept
{
    public string Payload { get; private set; }
    public string Request { get; private set; }
    public string ExpectedBehavior { get; private set; }
    public string ActualBehavior { get; private set; }
    
    public ProofOfConcept(string payload, string request, string expectedBehavior, string actualBehavior)
    {
        Payload = payload;
        Request = request;
        ExpectedBehavior = expectedBehavior;
        ActualBehavior = actualBehavior;
    }
}

/// <summary>
/// Severity (from bug-hunter)
/// </summary>
public enum Severity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Canary rollout strategy (from bug-hunter)
/// </summary>
public class CanaryRolloutStrategy
{
    public Guid Id { get; private set; }
    public List<Guid> CanaryBugIds { get; private set; }
    public List<Guid> RolloutBugIds { get; private set; }
    public bool CanaryPassed { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    public CanaryRolloutStrategy()
    {
        Id = Guid.NewGuid();
        CanaryBugIds = new List<Guid>();
        RolloutBugIds = new List<Guid>();
        CanaryPassed = false;
        StartedAt = DateTime.UtcNow;
    }
    
    public void AddCanaryBug(Guid bugId)
    {
        CanaryBugIds.Add(bugId);
    }
    
    public void AddRolloutBug(Guid bugId)
    {
        RolloutBugIds.Add(bugId);
    }
    
    public void MarkCanaryPassed()
    {
        CanaryPassed = true;
    }
    
    public void Complete()
    {
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Warp Drive object type (from do-things)
/// </summary>
public enum WarpDriveObjectType
{
    Prompt,
    Notebook,
    Workflow,
    Folder
}

/// <summary>
/// Warp Drive object (from do-things)
/// </summary>
public class WarpDriveObject
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public WarpDriveObjectType Type { get; private set; }
    public string Content { get; private set; }
    public List<string> Tags { get; private set; }
    public string ShareLink { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public WarpDriveObject(string name, WarpDriveObjectType type, string content)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        Content = content;
        Tags = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddTag(string tag)
    {
        if (!Tags.Contains(tag))
        {
            Tags.Add(tag);
        }
    }
}

/// <summary>
/// Job board (from job-ops)
/// </summary>
public class JobBoard
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Platform { get; private set; }
    public string Focus { get; private set; }
    public bool IsActive { get; private set; }
    
    public JobBoard(string name, string platform, string focus)
    {
        Id = Guid.NewGuid();
        Name = name;
        Platform = platform;
        Focus = focus;
        IsActive = true;
    }
}

/// <summary>
/// Job application (from job-ops)
/// </summary>
public class JobApplication
{
    public Guid Id { get; private set; }
    public string JobTitle { get; private set; }
    public string Company { get; private set; }
    public string JobBoard { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public int FitScore { get; private set; }
    public DateTime AppliedAt { get; private set; }
    public DateTime? LastUpdated { get; private set; }
    public string? CvVersion { get; private set; }
    
    public JobApplication(string jobTitle, string company, string jobBoard)
    {
        Id = Guid.NewGuid();
        JobTitle = jobTitle;
        Company = company;
        JobBoard = jobBoard;
        Status = ApplicationStatus.Applied;
        FitScore = 0;
        AppliedAt = DateTime.UtcNow;
    }
    
    public void UpdateStatus(ApplicationStatus status)
    {
        Status = status;
        LastUpdated = DateTime.UtcNow;
    }
    
    public void SetFitScore(int score)
    {
        FitScore = Math.Max(0, Math.Min(100, score));
    }
}

/// <summary>
/// Application status (from job-ops)
/// </summary>
public enum ApplicationStatus
{
    Applied,
    Interviewing,
    Offer,
    Rejected,
    Withdrawn
}

/// <summary>
/// Development skill (from superpowers)
/// </summary>
public class DevelopmentSkill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public string Description { get; private set; }
    public List<string> Triggers { get; private set; }
    public bool IsMandatory { get; private set; }
    
    public DevelopmentSkill(string name, string category, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Category = category;
        Description = description;
        Triggers = new List<string>();
        IsMandatory = false;
    }
    
    public void AddTrigger(string trigger)
    {
        Triggers.Add(trigger);
    }
    
    public void SetMandatory()
    {
        IsMandatory = true;
    }
}

/// <summary>
/// TDD cycle phase (from superpowers)
/// </summary>
public enum TddPhase
{
    Red,
    Green,
    Refactor
}

/// <summary>
/// TDD checkpoint (from superpowers)
/// </summary>
public class TddCheckpoint
{
    public Guid Id { get; private set; }
    public string TestFilePath { get; private set; }
    public string ImplementationFilePath { get; private set; }
    public TddPhase CurrentPhase { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsCompleted { get; private set; }
    
    public TddCheckpoint(string testFilePath, string implementationFilePath)
    {
        Id = Guid.NewGuid();
        TestFilePath = testFilePath;
        ImplementationFilePath = implementationFilePath;
        CurrentPhase = TddPhase.Red;
        CreatedAt = DateTime.UtcNow;
        IsCompleted = false;
    }
    
    public void AdvancePhase()
    {
        switch (CurrentPhase)
        {
            case TddPhase.Red:
                CurrentPhase = TddPhase.Green;
                break;
            case TddPhase.Green:
                CurrentPhase = TddPhase.Refactor;
                break;
            case TddPhase.Refactor:
                IsCompleted = true;
                break;
        }
    }
}

/// <summary>
/// Git worktree (from superpowers)
/// </summary>
public class GitWorktree
{
    public Guid Id { get; private set; }
    public string Path { get; private set; }
    public string BranchName { get; private set; }
    public Guid ParentCommit { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }
    
    public GitWorktree(string path, string branchName, Guid parentCommit)
    {
        Id = Guid.NewGuid();
        Path = path;
        BranchName = branchName;
        ParentCommit = parentCommit;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
}

/// <summary>
/// AI team member (from vibe-tools)
/// </summary>
public class AiTeamMember
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Role { get; private set; }
    public string Provider { get; private set; }
    public string Model { get; private set; }
    public List<string> Capabilities { get; private set; }
    public bool IsActive { get; private set; }
    
    public AiTeamMember(string name, string role, string provider, string model)
    {
        Id = Guid.NewGuid();
        Name = name;
        Role = role;
        Provider = provider;
        Model = model;
        Capabilities = new List<string>();
        IsActive = true;
    }
    
    public void AddCapability(string capability)
    {
        if (!Capabilities.Contains(capability))
        {
            Capabilities.Add(capability);
        }
    }
}

/// <summary>
/// AI team (from vibe-tools)
/// </summary>
public class AiTeam
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public List<AiTeamMember> Members { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public AiTeam(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Members = new List<AiTeamMember>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddMember(AiTeamMember member)
    {
        Members.Add(member);
    }
    
    public AiTeamMember? GetMemberByRole(string role)
    {
        return Members.FirstOrDefault(m => m.Role == role);
    }
}

/// <summary>
/// Browser automation command (from vibe-tools)
/// </summary>
public enum BrowserCommandType
{
    Open,
    Act,
    Observe,
    Extract,
    MacChrome
}

/// <summary>
/// Browser automation action (from vibe-tools)
/// </summary>
public class BrowserAutomationAction
{
    public Guid Id { get; private set; }
    public BrowserCommandType CommandType { get; private set; }
    public string Description { get; private set; }
    public string Url { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public object? Result { get; private set; }
    
    public BrowserAutomationAction(BrowserCommandType commandType, string description, string url)
    {
        Id = Guid.NewGuid();
        CommandType = commandType;
        Description = description;
        Url = url;
        Parameters = new Dictionary<string, object>();
        ExecutedAt = DateTime.UtcNow;
    }
    
    public void AddParameter(string key, object value)
    {
        Parameters[key] = value;
    }
    
    public void SetResult(object result)
    {
        Result = result;
    }
}

/// <summary>
/// Repository context query (from vibe-tools)
/// </summary>
public class RepositoryContextQuery
{
    public Guid Id { get; private set; }
    public string Query { get; private set; }
    public List<string> IncludedFiles { get; private set; }
    public List<string> ExcludedFiles { get; private set; }
    public bool IncludeGitDiff { get; private set; }
    public string? BaseBranch { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public RepositoryContextQuery(string query)
    {
        Id = Guid.NewGuid();
        Query = query;
        IncludedFiles = new List<string>();
        ExcludedFiles = new List<string>();
        IncludeGitDiff = false;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddIncludedFile(string filePath)
    {
        IncludedFiles.Add(filePath);
    }
    
    public void AddExcludedFile(string filePath)
    {
        ExcludedFiles.Add(filePath);
    }
    
    public void EnableGitDiff(string? baseBranch = null)
    {
        IncludeGitDiff = true;
        BaseBranch = baseBranch;
    }
}

/// <summary>
/// Documentation generation task (from vibe-tools)
/// </summary>
public class DocumentationGenerationTask
{
    public Guid Id { get; private set; }
    public string RepositoryUrl { get; private set; }
    public string OutputPath { get; private set; }
    public string? SpecificTask { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public TaskStatus Status { get; private set; }
    
    public DocumentationGenerationTask(string repositoryUrl, string outputPath)
    {
        Id = Guid.NewGuid();
        RepositoryUrl = repositoryUrl;
        OutputPath = outputPath;
        CreatedAt = DateTime.UtcNow;
        Status = TaskStatus.Pending;
    }
    
    public void SetSpecificTask(string task)
    {
        SpecificTask = task;
    }
    
    public void MarkCompleted()
    {
        Status = TaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Code generation phase (from vibesdk)
/// </summary>
public enum CodeGenerationPhase
{
    Planning,
    Foundation,
    Core,
    Styling,
    Integration,
    Optimization
}

/// <summary>
/// Phase-wise code generation (from vibesdk)
/// </summary>
public class PhaseWiseCodeGeneration
{
    public Guid Id { get; private set; }
    public string ProjectName { get; private set; }
    public Dictionary<CodeGenerationPhase, PhaseResult> PhaseResults { get; private set; }
    public CodeGenerationPhase CurrentPhase { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    public PhaseWiseCodeGeneration(string projectName)
    {
        Id = Guid.NewGuid();
        ProjectName = projectName;
        PhaseResults = new Dictionary<CodeGenerationPhase, PhaseResult>();
        CurrentPhase = CodeGenerationPhase.Planning;
        StartedAt = DateTime.UtcNow;
    }
    
    public void CompletePhase(CodeGenerationPhase phase, PhaseResult result)
    {
        PhaseResults[phase] = result;
        if (phase == CurrentPhase)
        {
            AdvancePhase();
        }
    }
    
    private void AdvancePhase()
    {
        if (CurrentPhase < CodeGenerationPhase.Optimization)
        {
            CurrentPhase++;
        }
        else
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Phase result (from vibesdk)
/// </summary>
public class PhaseResult
{
    public Guid Id { get; private set; }
    public CodeGenerationPhase Phase { get; private set; }
    public bool IsSuccessful { get; private set; }
    public List<string> GeneratedFiles { get; private set; }
    public List<string> Errors { get; private set; }
    public DateTime CompletedAt { get; private set; }
    
    public PhaseResult(CodeGenerationPhase phase)
    {
        Id = Guid.NewGuid();
        Phase = phase;
        GeneratedFiles = new List<string>();
        Errors = new List<string>();
        CompletedAt = DateTime.UtcNow;
    }
    
    public void AddGeneratedFile(string filePath)
    {
        GeneratedFiles.Add(filePath);
    }
    
    public void AddError(string error)
    {
        Errors.Add(error);
    }
}

/// <summary>
/// Live preview container (from vibesdk)
/// </summary>
public class LivePreviewContainer
{
    public Guid Id { get; private set; }
    public string ContainerId { get; private set; }
    public string PreviewUrl { get; private set; }
    public string InstanceType { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }
    
    public LivePreviewContainer(string containerId, string previewUrl, string instanceType)
    {
        Id = Guid.NewGuid();
        ContainerId = containerId;
        PreviewUrl = previewUrl;
        InstanceType = instanceType;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
}

/// <summary>
/// Agent workflow step (from warp)
/// </summary>
public enum AgentWorkflowStep
{
    Triage,
    SpecWriting,
    Implementation,
    CodeReview,
    Deployment
}

/// <summary>
/// Agent workflow (from warp)
/// </summary>
public class AgentWorkflow
{
    public Guid Id { get; private set; }
    public string IssueId { get; private set; }
    public Dictionary<AgentWorkflowStep, WorkflowStepResult> StepResults { get; private set; }
    public AgentWorkflowStep CurrentStep { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    public AgentWorkflow(string issueId)
    {
        Id = Guid.NewGuid();
        IssueId = issueId;
        StepResults = new Dictionary<AgentWorkflowStep, WorkflowStepResult>();
        CurrentStep = AgentWorkflowStep.Triage;
        StartedAt = DateTime.UtcNow;
    }
    
    public void CompleteStep(AgentWorkflowStep step, WorkflowStepResult result)
    {
        StepResults[step] = result;
        if (step == CurrentStep)
        {
            AdvanceStep();
        }
    }
    
    private void AdvanceStep()
    {
        if (CurrentStep < AgentWorkflowStep.Deployment)
        {
            CurrentStep++;
        }
        else
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Workflow step result (from warp)
/// </summary>
public class WorkflowStepResult
{
    public Guid Id { get; private set; }
    public AgentWorkflowStep Step { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string Output { get; private set; }
    public List<string> Artifacts { get; private set; }
    public DateTime CompletedAt { get; private set; }
    
    public WorkflowStepResult(AgentWorkflowStep step)
    {
        Id = Guid.NewGuid();
        Step = step;
        Artifacts = new List<string>();
        CompletedAt = DateTime.UtcNow;
    }
    
    public void AddArtifact(string artifact)
    {
        Artifacts.Add(artifact);
    }
}

/// <summary>
/// CLI agent integration (from warp)
/// </summary>
public class CliAgentIntegration
{
    public Guid Id { get; private set; }
    public string AgentType { get; private set; }
    public string ExecutablePath { get; private set; }
    public Dictionary<string, string> Configuration { get; private set; }
    public bool IsActive { get; private set; }
    
    public CliAgentIntegration(string agentType, string executablePath)
    {
        Id = Guid.NewGuid();
        AgentType = agentType;
        ExecutablePath = executablePath;
        Configuration = new Dictionary<string, string>();
        IsActive = true;
    }
    
    public void SetConfig(string key, string value)
    {
        Configuration[key] = value;
    }
}

/// <summary>
/// Deep learning model (from mxnet)
/// </summary>
public class DeepLearningModel
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Framework { get; private set; }
    public ProgrammingModel ProgrammingModel { get; private set; }
    public List<string> InputLayers { get; private set; }
    public List<string> OutputLayers { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public DeepLearningModel(string name, string framework, ProgrammingModel programmingModel)
    {
        Id = Guid.NewGuid();
        Name = name;
        Framework = framework;
        ProgrammingModel = programmingModel;
        InputLayers = new List<string>();
        OutputLayers = new List<string>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddInputLayer(string layerName)
    {
        InputLayers.Add(layerName);
    }
    
    public void AddOutputLayer(string layerName)
    {
        OutputLayers.Add(layerName);
    }
}

/// <summary>
/// Programming model (from mxnet)
/// </summary>
public enum ProgrammingModel
{
    Symbolic,
    Imperative,
    Hybrid
}

/// <summary>
/// Distributed training configuration (from mxnet)
/// </summary>
public class DistributedTrainingConfig
{
    public Guid Id { get; private set; }
    public int NumGpus { get; private set; }
    public int NumNodes { get; private set; }
    public string Backend { get; private set; }
    public bool AutoParallelism { get; private set; }
    public Dictionary<string, string> AdditionalConfig { get; private set; }
    
    public DistributedTrainingConfig(int numGpus, int numNodes, string backend)
    {
        Id = Guid.NewGuid();
        NumGpus = numGpus;
        NumNodes = numNodes;
        Backend = backend;
        AutoParallelism = true;
        AdditionalConfig = new Dictionary<string, string>();
    }
    
    public void SetConfig(string key, string value)
    {
        AdditionalConfig[key] = value;
    }
}

/// <summary>
/// Graph optimization layer (from mxnet)
/// </summary>
public class GraphOptimizationLayer
{
    public Guid Id { get; private set; }
    public List<OptimizationPass> OptimizationPasses { get; private set; }
    public bool IsEnabled { get; private set; }
    
    public GraphOptimizationLayer()
    {
        Id = Guid.NewGuid();
        OptimizationPasses = new List<OptimizationPass>();
        IsEnabled = true;
    }
    
    public void AddOptimizationPass(OptimizationPass pass)
    {
        OptimizationPasses.Add(pass);
    }
}

/// <summary>
/// Optimization pass (from mxnet)
/// </summary>
public class OptimizationPass
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Order { get; private set; }
    
    public OptimizationPass(string name, string description, int order)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Order = order;
    }
}

/// <summary>
/// Online learning model (from vowpal_wabbit)
/// </summary>
public class OnlineLearningModel
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public LearningAlgorithm Algorithm { get; private set; }
    public bool UseHashingTrick { get; private set; }
    public int FeatureHashBits { get; private set; }
    public double LearningRate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public OnlineLearningModel(string name, LearningAlgorithm algorithm)
    {
        Id = Guid.NewGuid();
        Name = name;
        Algorithm = algorithm;
        UseHashingTrick = true;
        FeatureHashBits = 18;
        LearningRate = 0.5;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetLearningRate(double rate)
    {
        LearningRate = Math.Max(0.0, Math.Min(1.0, rate));
    }
}

/// <summary>
/// Learning algorithm (from vowpal_wabbit)
/// </summary>
public enum LearningAlgorithm
{
    SparseGradientDescent,
    Adagrad,
    AdaDelta,
    Adam,
    FtrlProximal
}

/// <summary>
/// Contextual bandit (from vowpal_wabbit)
/// </summary>
public class ContextualBandit
{
    public Guid Id { get; private set; }
    public BanditAlgorithm Algorithm { get; private set; }
    public Dictionary<string, double> ContextFeatures { get; private set; }
    public List<BanditArm> Arms { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public ContextualBandit(BanditAlgorithm algorithm)
    {
        Id = Guid.NewGuid();
        Algorithm = algorithm;
        ContextFeatures = new Dictionary<string, double>();
        Arms = new List<BanditArm>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void AddContextFeature(string feature, double value)
    {
        ContextFeatures[feature] = value;
    }
    
    public void AddArm(BanditArm arm)
    {
        Arms.Add(arm);
    }
}

/// <summary>
/// Bandit algorithm (from vowpal_wabbit)
/// </summary>
public enum BanditAlgorithm
{
    EpsilonGreedy,
    UpperConfidenceBound,
    ThompsonSampling,
    Cover,
    Exp4
}

/// <summary>
/// Bandit arm (from vowpal_wabbit)
/// </summary>
public class BanditArm
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public double EstimatedReward { get; private set; }
    public int PullCount { get; private set; }
    
    public BanditArm(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        EstimatedReward = 0.0;
        PullCount = 0;
    }
    
    public void RecordPull(double reward)
    {
        PullCount++;
        // Update estimated reward using running average
        EstimatedReward = EstimatedReward + (reward - EstimatedReward) / PullCount;
    }
}

/// <summary>
/// Feature interaction (from vowpal_wabbit)
/// </summary>
public class FeatureInteraction
{
    public Guid Id { get; private set; }
    public List<string> FeatureNamespaces { get; private set; }
    public InteractionType Type { get; private set; }
    
    public FeatureInteraction(List<string> featureNamespaces, InteractionType type)
    {
        Id = Guid.NewGuid();
        FeatureNamespaces = featureNamespaces;
        Type = type;
    }
}

/// <summary>
/// Interaction type (from vowpal_wabbit)
/// </summary>
public enum InteractionType
{
    Quadratic,
    Cubic,
    CrossProduct
}

/// <summary>
/// MCP server (from n8n-mcp, mcp)
/// </summary>
public class McpServer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public string Endpoint { get; private set; }
    public List<McpTool> Tools { get; private set; }
    public Dictionary<string, string> Configuration { get; private set; }
    public bool IsActive { get; private set; }
    
    public McpServer(string name, string type, string endpoint)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        Endpoint = endpoint;
        Tools = new List<McpTool>();
        Configuration = new Dictionary<string, string>();
        IsActive = true;
    }
    
    public void AddTool(McpTool tool)
    {
        Tools.Add(tool);
    }
    
    public void SetConfig(string key, string value)
    {
        Configuration[key] = value;
    }
}

/// <summary>
/// MCP tool (from n8n-mcp)
/// </summary>
public class McpTool
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Dictionary<string, McpParameter> Parameters { get; private set; }
    public List<string> Examples { get; private set; }
    
    public McpTool(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Parameters = new Dictionary<string, McpParameter>();
        Examples = new List<string>();
    }
    
    public void AddParameter(McpParameter parameter)
    {
        Parameters[parameter.Name] = parameter;
    }
    
    public void AddExample(string example)
    {
        Examples.Add(example);
    }
}

/// <summary>
/// MCP parameter (from n8n-mcp)
/// </summary>
public class McpParameter
{
    public string Name { get; private set; }
    public string Type { get; private set; }
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public string? Description { get; private set; }
    
    public McpParameter(string name, string type, bool isRequired)
    {
        Name = name;
        Type = type;
        IsRequired = isRequired;
    }
}

/// <summary>
/// Node validation profile (from n8n-mcp)
/// </summary>
public enum NodeValidationProfile
{
    Minimal,
    Runtime,
    AiFriendly,
    Strict
}

/// <summary>
/// Node validation result (from n8n-mcp)
/// </summary>
public class NodeValidationResult
{
    public Guid Id { get; private set; }
    public string NodeType { get; private set; }
    public bool IsValid { get; private set; }
    public List<ValidationError> Errors { get; private set; }
    public List<ValidationWarning> Warnings { get; private set; }
    public List<ValidationFix> SuggestedFixes { get; private set; }
    
    public NodeValidationResult(string nodeType)
    {
        Id = Guid.NewGuid();
        NodeType = nodeType;
        Errors = new List<ValidationError>();
        Warnings = new List<ValidationWarning>();
        SuggestedFixes = new List<ValidationFix>();
    }
    
    public void AddError(ValidationError error)
    {
        Errors.Add(error);
    }
    
    public void AddWarning(ValidationWarning warning)
    {
        Warnings.Add(warning);
    }
    
    public void AddSuggestedFix(ValidationFix fix)
    {
        SuggestedFixes.Add(fix);
    }
}

/// <summary>
/// Validation error (from n8n-mcp)
/// </summary>
public class ValidationError
{
    public string Field { get; private set; }
    public string Message { get; private set; }
    public string Severity { get; private set; }
    
    public ValidationError(string field, string message, string severity = "error")
    {
        Field = field;
        Message = message;
        Severity = severity;
    }
}

/// <summary>
/// Validation warning (from n8n-mcp)
/// </summary>
public class ValidationWarning
{
    public string Field { get; private set; }
    public string Message { get; private set; }
    
    public ValidationWarning(string field, string message)
    {
        Field = field;
        Message = message;
    }
}

/// <summary>
/// Validation fix (from n8n-mcp)
/// </summary>
public class ValidationFix
{
    public string Field { get; private set; }
    public string SuggestedValue { get; private set; }
    public string Reason { get; private set; }
    
    public ValidationFix(string field, string suggestedValue, string reason)
    {
        Field = field;
        SuggestedValue = suggestedValue;
        Reason = reason;
    }
}

/// <summary>
/// Workflow template (from n8n-mcp)
/// </summary>
public class WorkflowTemplate
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string AuthorName { get; private set; }
    public string AuthorUsername { get; private set; }
    public string Url { get; private set; }
    public List<string> NodeTypes { get; private set; }
    public string Complexity { get; private set; }
    public int MaxSetupMinutes { get; private set; }
    public string? TargetAudience { get; private set; }
    public string? RequiredService { get; private set; }
    
    public WorkflowTemplate(string name, string authorName, string authorUsername, string url)
    {
        Id = Guid.NewGuid();
        Name = name;
        AuthorName = authorName;
        AuthorUsername = authorUsername;
        Url = url;
        NodeTypes = new List<string>();
        Complexity = "simple";
        MaxSetupMinutes = 30;
    }
    
    public void AddNodeType(string nodeType)
    {
        if (!NodeTypes.Contains(nodeType))
        {
            NodeTypes.Add(nodeType);
        }
    }
}

/// <summary>
/// Browser automation session (from mcp)
/// </summary>
public class BrowserAutomationSession
{
    public Guid Id { get; private set; }
    public string ProfileName { get; private set; }
    public bool IsLocalExecution { get; private set; }
    public bool IsStealthMode { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public List<BrowserAction> Actions { get; private set; }
    
    public BrowserAutomationSession(string profileName)
    {
        Id = Guid.NewGuid();
        ProfileName = profileName;
        IsLocalExecution = true;
        IsStealthMode = true;
        StartedAt = DateTime.UtcNow;
        Actions = new List<BrowserAction>();
    }
    
    public void AddAction(BrowserAction action)
    {
        Actions.Add(action);
    }
    
    public void EndSession()
    {
        EndedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Browser action (from mcp)
/// </summary>
public class BrowserAction
{
    public Guid Id { get; private set; }
    public string ActionType { get; private set; }
    public string Selector { get; private set; }
    public string? Value { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    
    public BrowserAction(string actionType, string selector)
    {
        Id = Guid.NewGuid();
        ActionType = actionType;
        Selector = selector;
        ExecutedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// CDP session (from browser-harness-js)
/// </summary>
public class CdpSession
{
    public Guid Id { get; private set; }
    public string TargetId { get; private set; }
    public string WsUrl { get; private set; }
    public DateTime ConnectedAt { get; private set; }
    public DateTime? DisconnectedAt { get; private set; }
    public bool IsActive { get; private set; }
    public Dictionary<string, object> State { get; private set; }
    
    public CdpSession(string targetId, string wsUrl)
    {
        Id = Guid.NewGuid();
        TargetId = targetId;
        WsUrl = wsUrl;
        ConnectedAt = DateTime.UtcNow;
        IsActive = true;
        State = new Dictionary<string, object>();
    }
    
    public void SetState(string key, object value)
    {
        State[key] = value;
    }
    
    public void Disconnect()
    {
        IsActive = false;
        DisconnectedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// CDP domain (from browser-harness-js)
/// </summary>
public class CdpDomain
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public List<CdpMethod> Methods { get; private set; }
    public List<CdpEvent> Events { get; private set; }
    
    public CdpDomain(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Methods = new List<CdpMethod>();
        Events = new List<CdpEvent>();
    }
    
    public void AddMethod(CdpMethod method)
    {
        Methods.Add(method);
    }
    
    public void AddEvent(CdpEvent evt)
    {
        Events.Add(evt);
    }
}

/// <summary>
/// CDP parameter (from browser-harness-js)
/// </summary>
public class CdpParameter
{
    public string Name { get; private set; }
    public string Type { get; private set; }
    public bool Required { get; private set; }
    
    public CdpParameter(string name, string type, bool required = false)
    {
        Name = name;
        Type = type;
        Required = required;
    }
}

/// <summary>
/// CDP method (from browser-harness-js)
/// </summary>
public class CdpMethod
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<CdpParameter> Parameters { get; private set; }
    public string ReturnType { get; private set; }
    
    public CdpMethod(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Parameters = new List<CdpParameter>();
    }
    
    public void AddParameter(CdpParameter parameter)
    {
        Parameters.Add(parameter);
    }
}

/// <summary>
/// CDP event (from browser-harness-js)
/// </summary>
public class CdpEvent
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public List<CdpParameter> Parameters { get; private set; }
    
    public CdpEvent(string name, string description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Parameters = new List<CdpParameter>();
    }
    
    public void AddParameter(CdpParameter parameter)
    {
        Parameters.Add(parameter);
    }
}

/// <summary>
/// CDP target (from browser-harness-js)
/// </summary>
public class CdpTarget
{
    public Guid Id { get; private set; }
    public string TargetId { get; private set; }
    public string Type { get; private set; }
    public string Title { get; private set; }
    public string Url { get; private set; }
    public bool IsAttached { get; private set; }
    
    public CdpTarget(string targetId, string type, string title, string url)
    {
        Id = Guid.NewGuid();
        TargetId = targetId;
        Type = type;
        Title = title;
        Url = url;
        IsAttached = false;
    }
    
    public void Attach()
    {
        IsAttached = true;
    }
    
    public void Detach()
    {
        IsAttached = false;
    }
}

/// <summary>
/// Browser audit result (from browser-tools-mcp)
/// </summary>
public class BrowserAuditResult
{
    public Guid Id { get; private set; }
    public AuditType AuditType { get; private set; }
    public double Score { get; private set; }
    public List<AuditIssue> Issues { get; private set; }
    public DateTime AuditedAt { get; private set; }
    
    public BrowserAuditResult(AuditType auditType)
    {
        Id = Guid.NewGuid();
        AuditType = auditType;
        Issues = new List<AuditIssue>();
        AuditedAt = DateTime.UtcNow;
    }
    
    public void AddIssue(AuditIssue issue)
    {
        Issues.Add(issue);
    }
}

/// <summary>
/// Audit type (from browser-tools-mcp)
/// </summary>
public enum AuditType
{
    Accessibility,
    Performance,
    SEO,
    BestPractices,
    NextJS
}

/// <summary>
/// Audit issue (from browser-tools-mcp)
/// </summary>
public class AuditIssue
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Severity { get; private set; }
    public string? CodeSnippets { get; private set; }
    
    public AuditIssue(string title, string description, string severity)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Severity = severity;
    }
}

/// <summary>
/// ARA Config (from AI-Research-SKILLs)
/// </summary>
public class AraConfig
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Content { get; private set; }
    public string Format { get; private set; }
    
    public AraConfig(string name, string content, string format)
    {
        Id = Guid.NewGuid();
        Name = name;
        Content = content;
        Format = format;
    }
}

/// <summary>
/// ARA Physical Layer (from AI-Research-SKILLs)
/// </summary>
public class AraPhysicalLayer
{
    public List<AraConfig> Configs { get; private set; }
    public List<AraCodeSnippet> CodeSnippets { get; private set; }
    
    public AraPhysicalLayer()
    {
        Configs = new List<AraConfig>();
        CodeSnippets = new List<AraCodeSnippet>();
    }
}

/// <summary>
/// ARA Code Snippet (from AI-Research-SKILLs)
/// </summary>
public class AraCodeSnippet
{
    public Guid Id { get; private set; }
    public string FilePath { get; private set; }
    public string Language { get; private set; }
    public string? Content { get; private set; }
    
    public AraCodeSnippet(string filePath, string language)
    {
        Id = Guid.NewGuid();
        FilePath = filePath;
        Language = language;
    }
}

/// <summary>
/// Code search result (from claude-context)
/// </summary>
public class CodeSearchResult
{
    public string RelativePath { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Content { get; set; }
    public double Score { get; set; }
    
    public CodeSearchResult()
    {
        RelativePath = string.Empty;
        Content = string.Empty;
        Score = 0;
    }
}

/// <summary>
/// LSP enforcement state (from claude-code-lsp-enforcement-kit)
/// </summary>
public class LspEnforcementState
{
    public Guid Id { get; private set; }
    public string ProjectPath { get; private set; }
    public bool WarmupDone { get; private set; }
    public int NavCount { get; private set; }
    public int ReadCount { get; private set; }
    public List<string> ReadFiles { get; private set; }
    public string LastTool { get; private set; }
    
    public LspEnforcementState(string projectPath)
    {
        Id = Guid.NewGuid();
        ProjectPath = projectPath;
        WarmupDone = false;
        NavCount = 0;
        ReadCount = 0;
        ReadFiles = new List<string>();
        LastTool = string.Empty;
    }
}

/// <summary>
/// PM skill category (from awesome-pm-skills)
/// </summary>
public enum PmSkillCategory
{
    Builder,
    Communicator,
    Strategist,
    Navigator,
    Leader,
    Measurement,
    Launch
}

/// <summary>
/// PM skill (from awesome-pm-skills)
/// </summary>
public class PmSkill
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public PmSkillCategory Category { get; private set; }
    public string Description { get; private set; }
    public List<string> Frameworks { get; private set; }
    
    public PmSkill(string name, PmSkillCategory category)
    {
        Id = Guid.NewGuid();
        Name = name;
        Category = category;
        Description = string.Empty;
        Frameworks = new List<string>();
    }
}

/// <summary>
/// PRD (Product Requirements Document) (from claude-task-master)
/// </summary>
public class ProductRequirementsDocument
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastParsedAt { get; private set; }
    public List<Guid> GeneratedTaskIds { get; private set; }
    
    public ProductRequirementsDocument(string title, string content)
    {
        Id = Guid.NewGuid();
        Title = title;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        GeneratedTaskIds = new List<Guid>();
    }
    
    public void MarkParsed()
    {
        LastParsedAt = DateTime.UtcNow;
    }
    
    public void AddGeneratedTask(Guid taskId)
    {
        GeneratedTaskIds.Add(taskId);
    }
}

/// <summary>
/// Autonomous development loop
/// </summary>
public class AutonomousDevelopmentLoop
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Circuit breaker for resilience
/// </summary>
public class CircuitBreaker
{
    public Guid Id { get; set; }
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// AI execution environment
/// </summary>
public class AiEnvironment
{
    public Guid Id { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
}

/// <summary>
/// Skill package for installation
/// </summary>
public class SkillPackage
{
    public Guid Id { get; set; }
    public string PackageName { get; set; } = string.Empty;
}

/// <summary>
/// Agent swarm for orchestration
/// </summary>
public class AgentSwarm
{
    public Guid Id { get; set; }
    public string OrchestratorType { get; set; } = string.Empty;
}

/// <summary>
/// AI task for orchestration
/// </summary>
public class AiTask
{
    public Guid Id { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Generative UI component descriptor
/// </summary>
public class GenerativeUiComponent
{
    public Guid Id { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PropsSchema { get; set; } = string.Empty;
}
