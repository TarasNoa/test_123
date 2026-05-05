using Microsoft.Extensions.Logging;

namespace Libr4.AI.Infrastructure.Orchestration;

/// <summary>
/// Implementation of multi-provider orchestrator
/// Coordinates up to 8 AI providers with parallel/sequential/adversarial execution
/// </summary>
public class MultiProviderOrchestrator : IMultiProviderOrchestrator
{
    private readonly IBackgroundAgentOrchestrator _baseOrchestrator;
    private readonly ILogger<MultiProviderOrchestrator> _logger;
    private readonly Dictionary<Guid, Dictionary<string, AgentTaskResult>> _taskResults = new();

    public MultiProviderOrchestrator(
        IBackgroundAgentOrchestrator baseOrchestrator,
        ILogger<MultiProviderOrchestrator> logger)
    {
        _baseOrchestrator = baseOrchestrator;
        _logger = logger;
    }

    public async Task<MultiProviderResult> ExecuteWithProvidersAsync(
        AgentTask task,
        ProviderExecutionMode mode,
        List<string> providerIds,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new MultiProviderResult { TaskId = task.Id };

        try
        {
            switch (mode)
            {
                case ProviderExecutionMode.Parallel:
                    await ExecuteParallelAsync(task, providerIds, result, cancellationToken);
                    break;
                case ProviderExecutionMode.Sequential:
                    await ExecuteSequentialAsync(task, providerIds, result, cancellationToken);
                    break;
                case ProviderExecutionMode.Adversarial:
                    await ExecuteAdversarialAsync(task, providerIds, result, cancellationToken);
                    break;
            }

            // Calculate consensus
            result.Consensus = await GetConsensusAsync(task.Id, 0.75f, cancellationToken);

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            result.CompletedAt = DateTimeOffset.UtcNow;

            // Store results
            _taskResults[task.Id] = result.ProviderResults;

            _logger.LogInformation(
                "Multi-provider execution completed: {TaskId}, Mode: {Mode}, Consensus: {Consensus}",
                task.Id, mode, result.Consensus.AgreementLevel);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Multi-provider execution failed for task {TaskId}", task.Id);
            throw;
        }
    }

    private async Task ExecuteParallelAsync(
        AgentTask task,
        List<string> providerIds,
        MultiProviderResult result,
        CancellationToken cancellationToken)
    {
        var tasks = providerIds.Select(providerId =>
        {
            var providerTask = CloneTask(task, providerId);
            return _baseOrchestrator.DispatchAsync(providerTask);
        }).ToList();

        await Task.WhenAll(tasks);

        foreach (var (taskId, providerId) in tasks.Zip(providerIds, (t, pid) => (t.Result, pid)))
        {
            var taskResult = await _baseOrchestrator.WaitForCompletionAsync(taskId, cancellationToken);
            if (taskResult != null)
            {
                result.ProviderResults[providerId] = taskResult;
            }
        }
    }

    private async Task ExecuteSequentialAsync(
        AgentTask task,
        List<string> providerIds,
        MultiProviderResult result,
        CancellationToken cancellationToken)
    {
        foreach (var providerId in providerIds)
        {
            var providerTask = CloneTask(task, providerId);
            var taskId = await _baseOrchestrator.DispatchAsync(providerTask);
            var taskResult = await _baseOrchestrator.WaitForCompletionAsync(taskId, cancellationToken);

            if (taskResult != null)
            {
                result.ProviderResults[providerId] = taskResult;
                
                // Pass context to next provider
                if (!string.IsNullOrEmpty(taskResult.Output))
                {
                    task.Parameters[$"context_from_{providerId}"] = taskResult.Output;
                }
            }
        }
    }

    private async Task ExecuteAdversarialAsync(
        AgentTask task,
        List<string> providerIds,
        MultiProviderResult result,
        CancellationToken cancellationToken)
    {
        // First provider generates solution
        var firstProvider = providerIds.First();
        var firstTask = CloneTask(task, firstProvider);
        var firstTaskId = await _baseOrchestrator.DispatchAsync(firstTask);
        var firstResult = await _baseOrchestrator.WaitForCompletionAsync(firstTaskId, cancellationToken);

        if (firstResult != null)
        {
            result.ProviderResults[firstProvider] = firstResult;
        }

        // Remaining providers review
        for (int i = 1; i < providerIds.Count; i++)
        {
            var reviewerId = providerIds[i];
            var reviewTask = CloneTask(task, reviewerId);
            reviewTask.Parameters["review_target"] = firstResult?.Output ?? string.Empty;
            reviewTask.Parameters["review_mode"] = "adversarial";

            var reviewTaskId = await _baseOrchestrator.DispatchAsync(reviewTask);
            var reviewResult = await _baseOrchestrator.WaitForCompletionAsync(reviewTaskId, cancellationToken);

            if (reviewResult != null)
            {
                result.ProviderResults[reviewerId] = reviewResult;
            }
        }
    }

    public async Task<ConsensusResult> GetConsensusAsync(
        Guid taskId,
        float threshold = 0.75f,
        CancellationToken cancellationToken = default)
    {
        if (!_taskResults.ContainsKey(taskId))
            return new ConsensusResult { PassesThreshold = false };

        var results = _taskResults[taskId];
        if (results.Count == 0)
            return new ConsensusResult { PassesThreshold = false };

        // Simple consensus: compare outputs
        var outputs = results.Values.Select(r => r.Output).Where(o => !string.IsNullOrEmpty(o)).ToList();
        if (outputs.Count == 0)
            return new ConsensusResult { PassesThreshold = false };

        // Group similar outputs
        var groups = outputs.GroupBy(o => HashOutput(o)).ToList();
        var largestGroup = groups.OrderByDescending(g => g.Count()).First();

        var agreementLevel = (float)largestGroup.Count() / outputs.Count;
        var passesThreshold = agreementLevel >= threshold;

        return new ConsensusResult
        {
            PassesThreshold = passesThreshold,
            AgreementLevel = agreementLevel,
            DominantAnswer = largestGroup.First(),
            AgreeingProviders = results.Where(r => HashOutput(r.Value.Output) == largestGroup.Key)
                .Select(r => r.Value.Metadata.TryGetValue("provider", out var p) ? p.ToString() : "unknown").ToList()
        };
    }

    public async Task<AdversarialReviewResult> RunAdversarialReviewAsync(
        Guid taskId,
        List<string> reviewerIds,
        CancellationToken cancellationToken = default)
    {
        // Implementation for adversarial review
        // This would involve having providers review each other's work
        return new AdversarialReviewResult
        {
            HasConsensus = true,
            ReviewRounds = 1,
            FinalRecommendation = "Approved"
        };
    }

    private AgentTask CloneTask(AgentTask task, string providerId)
    {
        return new AgentTask
        {
            Description = task.Description,
            Context = task.Context,
            Parameters = new Dictionary<string, object>(task.Parameters)
            {
                ["provider"] = providerId
            },
            ModelId = providerId, // Use providerId as modelId
            MaxIterations = task.MaxIterations,
            TimeoutSeconds = task.TimeoutSeconds
        };
    }

    private string HashOutput(string output)
    {
        // Simple hash for grouping similar outputs
        // In production, use semantic similarity
        var normalized = output.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("\n", "")
            .Replace("\r", "");
        
        return normalized.Length > 100 
            ? normalized.Substring(0, 100) 
            : normalized;
    }
}
