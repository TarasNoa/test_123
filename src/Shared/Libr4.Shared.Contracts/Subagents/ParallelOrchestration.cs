namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Represents a parallel execution plan.
/// </summary>
public record ParallelExecutionPlan
{
    /// <summary>
    /// Unique identifier for the plan.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Plan name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Plan description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Execution groups (can run in parallel within each group, but groups run sequentially).
    /// </summary>
    public List<ExecutionGroup> Groups { get; init; } = new();

    /// <summary>
    /// Plan metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Represents a group of subagents that can run in parallel.
/// </summary>
public record ExecutionGroup
{
    /// <summary>
    /// Group name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Subagent executions in this group.
    /// </summary>
    public List<SubagentExecutionRequest> Executions { get; init; } = new();

    /// <summary>
    /// Whether to continue on error within the group.
    /// </summary>
    public bool ContinueOnError { get; init; } = false;

    /// <summary>
    /// Maximum parallel executions in this group.
    /// </summary>
    public int MaxParallelism { get; init; } = 4;
}

/// <summary>
/// Represents a request to execute a subagent.
/// </summary>
public record SubagentExecutionRequest
{
    /// <summary>
    /// Subagent ID.
    /// </summary>
    public string SubagentId { get; init; } = string.Empty;

    /// <summary>
    /// Input for the subagent.
    /// </summary>
    public string Input { get; init; } = string.Empty;

    /// <summary>
    /// Additional context.
    /// </summary>
    public Dictionary<string, object>? Context { get; init; }

    /// <summary>
    /// Priority of this execution.
    /// </summary>
    public ExecutionPriority Priority { get; init; } = ExecutionPriority.Normal;

    /// <summary>
    /// Timeout for this execution.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Dependencies on other executions within the same group.
    /// </summary>
    public List<string> DependsOn { get; init; } = new();
}

/// <summary>
/// Priority of execution.
/// </summary>
public enum ExecutionPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Result of parallel execution.
/// </summary>
public record ParallelExecutionResult
{
    /// <summary>
    /// Plan ID.
    /// </summary>
    public string PlanId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the entire plan succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Results from each group.
    /// </summary>
    public List<GroupExecutionResult> GroupResults { get; init; } = new();

    /// <summary>
    /// Total execution duration.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// When the plan started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the plan completed.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    public int SuccessfulExecutions { get; init; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    public int FailedExecutions { get; init; }
}

/// <summary>
/// Result of a group execution.
/// </summary>
public record GroupExecutionResult
{
    /// <summary>
    /// Group name.
    /// </summary>
    public string GroupName { get; init; } = string.Empty;

    /// <summary>
    /// Individual execution results.
    /// </summary>
    public List<SubagentExecutionResult> ExecutionResults { get; init; } = new();

    /// <summary>
    /// Whether the group succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Group duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When the group started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the group completed.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for parallel subagent orchestrator.
/// </summary>
public interface IParallelSubagentOrchestrator
{
    /// <summary>
    /// Executes a parallel execution plan.
    /// </summary>
    /// <param name="plan">Execution plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parallel execution result.</returns>
    Task<ParallelExecutionResult> ExecutePlanAsync(
        ParallelExecutionPlan plan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a simple parallel plan from subagent IDs.
    /// </summary>
    /// <param name="subagentIds">Subagent IDs to execute in parallel.</param>
    /// <param name="input">Input for all subagents.</param>
    /// <param name="context">Additional context.</param>
    /// <returns>Execution plan.</returns>
    ParallelExecutionPlan CreateSimpleParallelPlan(
        List<string> subagentIds,
        string input,
        Dictionary<string, object>? context = null);

    /// <summary>
    /// Creates a grouped execution plan.
    /// </summary>
    /// <param name="name">Plan name.</param>
    /// <param name="description">Plan description.</param>
    /// <returns>Execution plan builder.</returns>
    IExecutionPlanBuilder CreatePlanBuilder(
        string name,
        string description);
}

/// <summary>
/// Interface for building execution plans.
/// </summary>
public interface IExecutionPlanBuilder
{
    /// <summary>
    /// Adds a new execution group.
    /// </summary>
    /// <param name="name">Group name.</param>
    /// <param name="configure">Configuration action for the group.</param>
    /// <returns>The builder.</returns>
    IExecutionPlanBuilder AddGroup(
        string name,
        Action<ExecutionGroupBuilder>? configure = null);

    /// <summary>
    /// Builds the execution plan.
    /// </summary>
    /// <returns>The execution plan.</returns>
    ParallelExecutionPlan Build();
}

/// <summary>
/// Builder for execution groups.
/// </summary>
public class ExecutionGroupBuilder
{
    private ExecutionGroup _group;

    public ExecutionGroupBuilder(string name)
    {
        _group = new ExecutionGroup { Name = name };
    }

    public ExecutionGroupBuilder AddExecution(
        string subagentId,
        string input,
        Dictionary<string, object>? context = null,
        ExecutionPriority priority = ExecutionPriority.Normal,
        TimeSpan? timeout = null)
    {
        _group = _group with
        {
            Executions = _group.Executions.Concat(new[] { new SubagentExecutionRequest
            {
                SubagentId = subagentId,
                Input = input,
                Context = context,
                Priority = priority,
                Timeout = timeout
            }}).ToList()
        };
        return this;
    }

    public ExecutionGroupBuilder WithContinueOnError(bool continueOnError = true)
    {
        _group = _group with { ContinueOnError = continueOnError };
        return this;
    }

    public ExecutionGroupBuilder WithMaxParallelism(int maxParallelism)
    {
        _group = _group with { MaxParallelism = maxParallelism };
        return this;
    }

    public ExecutionGroup Build() => _group;
}

/// <summary>
/// In-memory implementation of parallel subagent orchestrator.
/// </summary>
public class InMemoryParallelSubagentOrchestrator : IParallelSubagentOrchestrator
{
    private readonly ISubagentExecutor _executor;

    public InMemoryParallelSubagentOrchestrator(ISubagentExecutor executor)
    {
        _executor = executor;
    }

    public async Task<ParallelExecutionResult> ExecutePlanAsync(
        ParallelExecutionPlan plan,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var groupResults = new List<GroupExecutionResult>();
        var totalSuccessful = 0;
        var totalFailed = 0;

        foreach (var group in plan.Groups)
        {
            var groupStartedAt = DateTime.UtcNow;
            var executionResults = new List<SubagentExecutionResult>();

            // Execute group with dependency resolution
            var executed = new HashSet<string>();
            var pending = group.Executions.ToDictionary(e => e.SubagentId, e => e);

            while (pending.Any())
            {
                // Find tasks with all dependencies satisfied
                var ready = pending
                    .Where(kvp => kvp.Value.DependsOn.All(dep => executed.Contains(dep)))
                    .ToList();

                if (!ready.Any())
                {
                    // Circular dependency or all remaining tasks have unmet dependencies
                    break;
                }

                // Execute ready tasks up to max parallelism
                var batch = ready.Take(group.MaxParallelism).ToList();
                var tasks = batch.Select(kvp => _executor.ExecuteAsync(
                    kvp.Value.SubagentId,
                    kvp.Value.Input,
                    kvp.Value.Context,
                    cancellationToken));

                var results = await Task.WhenAll(tasks);

                foreach (var (subagentId, request) in batch)
                {
                    var result = results.FirstOrDefault(r => r.SubagentId == subagentId);
                    if (result != null)
                    {
                        executionResults.Add(result);
                        executed.Add(subagentId);
                        pending.Remove(subagentId);

                        if (result.Success)
                        {
                            totalSuccessful++;
                        }
                        else
                        {
                            totalFailed++;
                            if (!group.ContinueOnError)
                            {
                                // Stop the group on first error
                                pending.Clear();
                                break;
                            }
                        }
                    }
                }
            }

            var groupCompletedAt = DateTime.UtcNow;
            groupResults.Add(new GroupExecutionResult
            {
                GroupName = group.Name,
                ExecutionResults = executionResults,
                Success = executionResults.All(r => r.Success) || group.ContinueOnError,
                Duration = groupCompletedAt - groupStartedAt,
                StartedAt = groupStartedAt,
                CompletedAt = groupCompletedAt
            });

            // Stop the plan if a group failed and continueOnError is false
            if (!groupResults.Last().Success && !group.ContinueOnError)
            {
                break;
            }
        }

        var completedAt = DateTime.UtcNow;

        return new ParallelExecutionResult
        {
            PlanId = plan.Id,
            Success = groupResults.All(g => g.Success),
            GroupResults = groupResults,
            TotalDuration = completedAt - startedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            SuccessfulExecutions = totalSuccessful,
            FailedExecutions = totalFailed
        };
    }

    public ParallelExecutionPlan CreateSimpleParallelPlan(
        List<string> subagentIds,
        string input,
        Dictionary<string, object>? context = null)
    {
        return new ParallelExecutionPlan
        {
            Name = "Simple Parallel Execution",
            Description = "Execute subagents in parallel",
            Groups = new List<ExecutionGroup>
            {
                new ExecutionGroup
                {
                    Name = "Group 1",
                    Executions = subagentIds.Select(id => new SubagentExecutionRequest
                    {
                        SubagentId = id,
                        Input = input,
                        Context = context
                    }).ToList()
                }
            }
        };
    }

    public IExecutionPlanBuilder CreatePlanBuilder(
        string name,
        string description)
    {
        return new ExecutionPlanBuilderImpl(name, description);
    }

    private class ExecutionPlanBuilderImpl : IExecutionPlanBuilder
    {
        private readonly ParallelExecutionPlan _plan;
        private readonly List<ExecutionGroup> _groups = new();

        public ExecutionPlanBuilderImpl(string name, string description)
        {
            _plan = new ParallelExecutionPlan
            {
                Name = name,
                Description = description
            };
        }

        public IExecutionPlanBuilder AddGroup(
            string name,
            Action<ExecutionGroupBuilder>? configure = null)
        {
            var builder = new ExecutionGroupBuilder(name);
            configure?.Invoke(builder);
            _groups.Add(builder.Build());
            return this;
        }

        public ParallelExecutionPlan Build()
        {
            return _plan with { Groups = _groups };
        }
    }
}
