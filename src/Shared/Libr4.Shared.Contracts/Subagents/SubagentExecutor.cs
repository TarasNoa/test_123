namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// In-memory implementation of subagent executor.
/// </summary>
public class InMemorySubagentExecutor : ISubagentExecutor
{
    private readonly ISubagentRegistry _registry;

    public InMemorySubagentExecutor(ISubagentRegistry registry)
    {
        _registry = registry;
    }

    public async Task<SubagentExecutionResult> ExecuteAsync(
        string subagentId,
        string input,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        var subagent = _registry.GetSubagent(subagentId);
        if (subagent == null)
        {
            throw new ArgumentException($"Subagent with ID {subagentId} not found", nameof(subagentId));
        }

        if (!subagent.Enabled)
        {
            return new SubagentExecutionResult
            {
                SubagentId = subagentId,
                Success = false,
                ErrorMessage = "Subagent is disabled",
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }

        var startedAt = DateTime.UtcNow;
        
        // In a real implementation, this would invoke the actual LLM with the subagent's instructions
        // For now, we simulate execution
        await Task.Delay(new Random().Next(100, 500), cancellationToken);
        
        var completedAt = DateTime.UtcNow;

        return new SubagentExecutionResult
        {
            SubagentId = subagentId,
            Success = true,
            Output = $"Simulated output from {subagent.Name} for input: {input}",
            Duration = completedAt - startedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            TokensUsed = new Random().Next(100, 1000)
        };
    }

    public async Task<IReadOnlyList<SubagentExecutionResult>> ExecuteParallelAsync(
        List<string> subagentIds,
        string input,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = subagentIds.Select(id => ExecuteAsync(id, input, context, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<SubagentExecutionResult>> ExecuteChainAsync(
        List<string> subagentIds,
        string input,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SubagentExecutionResult>();
        var currentInput = input;

        foreach (var subagentId in subagentIds)
        {
            var result = await ExecuteAsync(subagentId, currentInput, context, cancellationToken);
            results.Add(result);

            if (!result.Success)
            {
                // Stop chain on failure
                break;
            }

            // Use output of current as input for next
            currentInput = result.Output;
        }

        return results.AsReadOnly();
    }
}
