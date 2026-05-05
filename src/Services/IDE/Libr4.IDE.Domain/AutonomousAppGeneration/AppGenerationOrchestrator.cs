using Libr4.IDE.Domain.AutonomousAppGeneration.Events;
using Libr4.IDE.Domain.Common;

namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Top-level orchestrator that sits above every other IDE agent.
/// It:
///   1. analyses the user request,
///   2. asks a planner LLM to pick a tech stack and decompose the work,
///   3. instantiates the required existing agents (SecurityTesting, SemanticBlame, ...),
///   4. drives the generate -> run-in-shadow -> test -> fix loop until the
///      application builds and all tests pass (or the max iteration budget is spent).
/// This aggregate stores the plan, every iteration performed and the final outcome.
/// </summary>
public sealed class AppGenerationOrchestrator : AggregateRoot<Guid>
{
    public string UserRequest { get; private set; } = null!;
    public string RequestFingerprint { get; private set; } = null!;
    public GenerationPlan? Plan { get; private set; }
    public GenerationStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// P2-3 of audit roadmap. Optional tenant identifier for multi-tenant partitioning.
    /// Null means the run belongs to the default/single-tenant deployment.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>Shadow workspace identifier used to run generated code.</summary>
    public Guid? ShadowWorkspaceId { get; private set; }
    /// <summary>Underlying multi-agent orchestration that coordinates individual agents.</summary>
    public Guid? MultiAgentOrchestrationId { get; private set; }

    public IReadOnlyList<IterationCycle> Iterations => _iterations.AsReadOnly();
    public IReadOnlyList<GeneratedFile> Files => _files.AsReadOnly();
    public IReadOnlyList<QualityGateSnapshot> QualityGates => _qualityGates.AsReadOnly();
    public IReadOnlyList<McpExecutionAuditEntry> McpExecutions => _mcpExecutions.AsReadOnly();
    public IReadOnlyList<MemoryIngestAuditEntry> MemoryIngests => _memoryIngests.AsReadOnly();
    public IReadOnlyList<MemoryRetrievalAuditEntry> MemoryRetrievals => _memoryRetrievals.AsReadOnly();
    public IReadOnlyList<SkillInvocationAuditEntry> SkillInvocations => _skillInvocations.AsReadOnly();
    public IReadOnlyList<AgentTaskGraphEntry> TaskGraph => _taskGraph.AsReadOnly();
    public IReadOnlyList<SecurityReviewAuditEntry> SecurityReviews => _securityReviews.AsReadOnly();
    public IReadOnlyList<CascadePlanAuditEntry> CascadePlans => _cascadePlans.AsReadOnly();
    public IReadOnlyList<CheckpointAuditEntry> Checkpoints => _checkpoints.AsReadOnly();
    public IReadOnlyList<TriggerIngestionAuditEntry> Triggers => _triggers.AsReadOnly();

    private readonly List<IterationCycle> _iterations = new();
    private readonly List<GeneratedFile> _files = new();
    private readonly List<QualityGateSnapshot> _qualityGates = new();
    private readonly List<McpExecutionAuditEntry> _mcpExecutions = new();
    private readonly List<MemoryIngestAuditEntry> _memoryIngests = new();
    private readonly List<MemoryRetrievalAuditEntry> _memoryRetrievals = new();
    private readonly List<SkillInvocationAuditEntry> _skillInvocations = new();
    private readonly List<AgentTaskGraphEntry> _taskGraph = new();
    private readonly List<SecurityReviewAuditEntry> _securityReviews = new();
    private readonly List<CascadePlanAuditEntry> _cascadePlans = new();
    private readonly List<CheckpointAuditEntry> _checkpoints = new();
    private readonly List<TriggerIngestionAuditEntry> _triggers = new();

    private AppGenerationOrchestrator() { }

    private AppGenerationOrchestrator(Guid id, string userRequest, string requestFingerprint)
    {
        Id = id;
        UserRequest = userRequest ?? throw new ArgumentNullException(nameof(userRequest));
        RequestFingerprint = requestFingerprint ?? throw new ArgumentNullException(nameof(requestFingerprint));
        Status = GenerationStatus.Planning;
        StartedAt = DateTime.UtcNow;
    }

    public static AppGenerationOrchestrator Create(string userRequest, string requestFingerprint)
    {
        if (string.IsNullOrWhiteSpace(userRequest))
            throw new ArgumentException("User request must be provided", nameof(userRequest));
        if (string.IsNullOrWhiteSpace(requestFingerprint))
            throw new ArgumentException("Request fingerprint must be provided", nameof(requestFingerprint));

        return new AppGenerationOrchestrator(Guid.NewGuid(), userRequest, requestFingerprint);
    }

    /// <summary>P2-3: assigns a tenant to this run; can only be set once before generation starts.</summary>
    public void SetTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId must not be blank", nameof(tenantId));
        if (TenantId is not null && TenantId != tenantId)
            throw new InvalidOperationException($"TenantId already set to '{TenantId}'; cannot reassign.");
        TenantId = tenantId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Stores the plan produced by the planner LLM.</summary>
    public void AttachPlan(GenerationPlan plan)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppGenerationPlannedEvent(
            Id, plan.ApplicationName, plan.Phases.Count, plan.RequiredAgents.Count));
    }

    /// <summary>Links this orchestrator with the shadow workspace that will execute the code.</summary>
    public void AttachShadowWorkspace(Guid shadowWorkspaceId)
    {
        if (shadowWorkspaceId == Guid.Empty)
            throw new ArgumentException("Shadow workspace id must be provided", nameof(shadowWorkspaceId));
        ShadowWorkspaceId = shadowWorkspaceId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Links this orchestrator with the underlying multi-agent orchestration.</summary>
    public void AttachMultiAgentOrchestration(Guid multiAgentOrchestrationId)
    {
        if (multiAgentOrchestrationId == Guid.Empty)
            throw new ArgumentException("Multi-agent orchestration id must be provided", nameof(multiAgentOrchestrationId));
        MultiAgentOrchestrationId = multiAgentOrchestrationId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void BeginGeneration()
    {
        if (Plan is null)
            throw new InvalidOperationException("Cannot start generation before a plan is attached");

        Status = GenerationStatus.Generating;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppGenerationStartedEvent(Id, Plan.ApplicationName));
    }

    /// <summary>Registers a newly generated or updated source file.</summary>
    public void UpsertFile(GeneratedFile file)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));

        var existing = _files.FirstOrDefault(f => f.RelativePath == file.RelativePath);
        if (existing is null)
        {
            _files.Add(file);
        }
        else
        {
            existing.Update(file.Content);
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Begins a new generate/test/fix iteration.</summary>
    public IterationCycle BeginIteration()
    {
        if (Plan is null)
            throw new InvalidOperationException("Cannot iterate without a plan");
        if (_iterations.Count >= Plan.MaxIterations)
            throw new InvalidOperationException("Iteration budget exhausted");

        var iteration = new IterationCycle(_iterations.Count + 1);
        _iterations.Add(iteration);
        Status = GenerationStatus.Testing;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new IterationStartedEvent(Id, iteration.Id, iteration.Number));
        return iteration;
    }

    /// <summary>Closes the current iteration with its execution outcome.</summary>
    public void CompleteIteration(Guid iterationId, ExecutionResult execution, IReadOnlyList<ErrorReport>? errors = null)
    {
        var iteration = _iterations.FirstOrDefault(i => i.Id == iterationId)
            ?? throw new InvalidOperationException($"Iteration {iterationId} not found");

        iteration.SetExecutionResult(execution);
        if (errors != null)
        {
            foreach (var err in errors) iteration.AddError(err);
        }

        Status = execution.Succeeded ? GenerationStatus.Completed : GenerationStatus.Fixing;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new IterationCompletedEvent(
            Id, iteration.Id, iteration.Number, execution.Succeeded, errors?.Count ?? 0));
    }

    public void MarkCompleted()
    {
        Status = GenerationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppGenerationCompletedEvent(
            Id, Plan?.ApplicationName ?? string.Empty, _iterations.Count));
    }

    public void MarkFailed(string reason)
    {
        Status = GenerationStatus.Failed;
        FailureReason = reason ?? "unknown";
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new AppGenerationFailedEvent(Id, FailureReason, _iterations.Count));
    }

    public void RecordQualityGate(string stage, int score, bool passed, IReadOnlyList<string>? reasons = null, CertaintyLevel certainty = CertaintyLevel.Medium)
    {
        _qualityGates.Add(new QualityGateSnapshot(stage, score, passed, reasons, certainty));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordMcpExecution(McpExecutionAuditEntry entry)
    {
        _mcpExecutions.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordMemoryIngest(MemoryIngestAuditEntry entry)
    {
        _memoryIngests.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordMemoryRetrieval(MemoryRetrievalAuditEntry entry)
    {
        _memoryRetrievals.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSkillInvocation(SkillInvocationAuditEntry entry)
    {
        _skillInvocations.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReplaceTaskGraph(IReadOnlyList<AgentTaskGraphEntry> tasks)
    {
        _taskGraph.Clear();
        _taskGraph.AddRange(tasks);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSecurityReview(SecurityReviewAuditEntry entry)
    {
        _securityReviews.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordCascadePlan(CascadePlanAuditEntry entry)
    {
        _cascadePlans.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordCheckpoint(CheckpointAuditEntry entry)
    {
        _checkpoints.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordTrigger(TriggerIngestionAuditEntry entry)
    {
        _triggers.Add(entry);
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanIterateMore() =>
        Plan != null && _iterations.Count < Plan.MaxIterations
        && Status != GenerationStatus.Completed && Status != GenerationStatus.Failed;
}
