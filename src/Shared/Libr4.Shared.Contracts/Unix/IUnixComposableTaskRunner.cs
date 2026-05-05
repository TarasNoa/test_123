namespace Libr4.Shared.Contracts.Unix;

/// <summary>
/// Represents a composable task step in a Unix-style pipeline.
/// </summary>
public record ComposableTaskStep
{
    /// <summary>
    /// Unique identifier for the step.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Step name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Command to execute.
    /// </summary>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Command arguments.
    /// </summary>
    public List<string> Arguments { get; init; } = new();

    /// <summary>
    /// Working directory for the command.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Environment variables for the command.
    /// </summary>
    public Dictionary<string, string> Environment { get; init; } = new();

    /// <summary>
    /// Timeout for the command.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Whether to continue on error.
    /// </summary>
    public bool ContinueOnError { get; init; }

    /// <summary>
    /// Dependencies on other step IDs.
    /// </summary>
    public List<string> DependsOn { get; init; } = new();
}

/// <summary>
/// Result of executing a composable task step.
/// </summary>
public record StepExecutionResult
{
    /// <summary>
    /// Step ID.
    /// </summary>
    public string StepId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the step succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Exit code.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Standard output.
    /// </summary>
    public string Stdout { get; init; } = string.Empty;

    /// <summary>
    /// Standard error.
    /// </summary>
    public string Stderr { get; init; } = string.Empty;

    /// <summary>
    /// Execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// When the step started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the step completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Error message if the step failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Represents a composable task pipeline.
/// </summary>
public record ComposableTaskPipeline
{
    /// <summary>
    /// Unique identifier for the pipeline.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Pipeline name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Pipeline description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Steps in the pipeline.
    /// </summary>
    public List<ComposableTaskStep> Steps { get; init; } = new();

    /// <summary>
    /// Whether to stop on first error.
    /// </summary>
    public bool StopOnError { get; init; } = true;

    /// <summary>
    /// Pipeline metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Result of executing a composable task pipeline.
/// </summary>
public record PipelineExecutionResult
{
    /// <summary>
    /// Pipeline ID.
    /// </summary>
    public string PipelineId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the pipeline succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Results of individual steps.
    /// </summary>
    public List<StepExecutionResult> StepResults { get; init; } = new();

    /// <summary>
    /// Total execution duration.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// When the pipeline started.
    /// </summary>
    public DateTime StartedAt { get; init; }

    /// <summary>
    /// When the pipeline completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Number of successful steps.
    /// </summary>
    public int SuccessfulSteps { get; init; }

    /// <summary>
    /// Number of failed steps.
    /// </summary>
    public int FailedSteps { get; init; }
}

/// <summary>
/// Interface for Unix-style composable task runner.
/// Allows scriptable pipeline steps with dependencies and error handling.
/// </summary>
public interface IUnixComposableTaskRunner
{
    /// <summary>
    /// Executes a single step.
    /// </summary>
    /// <param name="step">Step to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Step execution result.</returns>
    Task<StepExecutionResult> ExecuteStepAsync(
        ComposableTaskStep step,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a pipeline of steps.
    /// </summary>
    /// <param name="pipeline">Pipeline to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pipeline execution result.</returns>
    Task<PipelineExecutionResult> ExecutePipelineAsync(
        ComposableTaskPipeline pipeline,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a pipeline before execution.
    /// </summary>
    /// <param name="pipeline">Pipeline to validate.</param>
    /// <returns>Validation result with errors if any.</returns>
    Task<ValidationResult> ValidatePipelineAsync(ComposableTaskPipeline pipeline);

    /// <summary>
    /// Creates a pipeline from a Unix-style script string.
    /// </summary>
    /// <param name="script">Unix-style script (e.g., "git pull && npm install && npm test").</param>
    /// <param name="name">Pipeline name.</param>
    /// <param name="workingDirectory">Working directory.</param>
    /// <returns>Parsed pipeline.</returns>
    Task<ComposableTaskPipeline> CreatePipelineFromScriptAsync(
        string script,
        string name,
        string? workingDirectory = null);
}

/// <summary>
/// Validation result.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// In-memory implementation of Unix composable task runner.
/// </summary>
public class InMemoryUnixComposableTaskRunner : IUnixComposableTaskRunner
{
    public Task<StepExecutionResult> ExecuteStepAsync(
        ComposableTaskStep step,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        
        // In a real implementation, this would execute the actual command
        // For now, we simulate execution
        var duration = TimeSpan.FromMilliseconds(new Random().Next(100, 1000));
        
        var result = new StepExecutionResult
        {
            StepId = step.Id,
            Success = true,
            ExitCode = 0,
            Stdout = $"Simulated output for {step.Command}",
            Stderr = string.Empty,
            Duration = duration,
            StartedAt = startedAt,
            CompletedAt = startedAt + duration
        };

        return Task.FromResult(result);
    }

    public async Task<PipelineExecutionResult> ExecutePipelineAsync(
        ComposableTaskPipeline pipeline,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        var stepResults = new List<StepExecutionResult>();
        var successfulSteps = 0;
        var failedSteps = 0;

        // Build dependency graph
        var stepMap = pipeline.Steps.ToDictionary(s => s.Id);
        var executedSteps = new HashSet<string>();

        foreach (var step in pipeline.Steps)
        {
            // Check dependencies
            if (step.DependsOn.Any(dep => !executedSteps.Contains(dep)))
            {
                continue;
            }

            var result = await ExecuteStepAsync(step, cancellationToken);
            stepResults.Add(result);

            if (result.Success)
            {
                successfulSteps++;
                executedSteps.Add(step.Id);
            }
            else
            {
                failedSteps++;
                if (pipeline.StopOnError && !step.ContinueOnError)
                {
                    break;
                }
                executedSteps.Add(step.Id);
            }
        }

        var completedAt = DateTime.UtcNow;

        return new PipelineExecutionResult
        {
            PipelineId = pipeline.Id,
            Success = failedSteps == 0,
            StepResults = stepResults,
            TotalDuration = completedAt - startedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            SuccessfulSteps = successfulSteps,
            FailedSteps = failedSteps
        };
    }

    public Task<ValidationResult> ValidatePipelineAsync(ComposableTaskPipeline pipeline)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check for circular dependencies
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var step in pipeline.Steps)
        {
            if (HasCircularDependency(step.Id, step, pipeline.Steps, visited, recursionStack))
            {
                errors.Add($"Circular dependency detected involving step {step.Id}");
            }
        }

        // Check for missing dependencies
        var stepIds = pipeline.Steps.Select(s => s.Id).ToHashSet();
        foreach (var step in pipeline.Steps)
        {
            foreach (var dep in step.DependsOn)
            {
                if (!stepIds.Contains(dep))
                {
                    warnings.Add($"Step {step.Id} depends on non-existent step {dep}");
                }
            }
        }

        // Check for empty steps
        if (!pipeline.Steps.Any())
        {
            errors.Add("Pipeline has no steps");
        }

        return Task.FromResult(new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        });
    }

    public Task<ComposableTaskPipeline> CreatePipelineFromScriptAsync(
        string script,
        string name,
        string? workingDirectory = null)
    {
        var steps = new List<ComposableTaskStep>();
        var commands = script.Split(new[] { "&&", "||", ";" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < commands.Length; i++)
        {
            var command = commands[i].Trim();
            if (string.IsNullOrEmpty(command)) continue;

            var parts = command.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0];
            var args = parts.Length > 1 ? parts[1].Split(' ').ToList() : new List<string>();

            var step = new ComposableTaskStep
            {
                Name = $"Step {i + 1}",
                Command = cmd,
                Arguments = args,
                WorkingDirectory = workingDirectory
            };

            // Add dependency on previous step
            if (i > 0 && steps.Any())
            {
                step = step with { DependsOn = new List<string> { steps[i - 1].Id } };
            }

            steps.Add(step);
        }

        var pipeline = new ComposableTaskPipeline
        {
            Name = name,
            Description = $"Pipeline created from script: {script}",
            Steps = steps
        };

        return Task.FromResult(pipeline);
    }

    private static bool HasCircularDependency(
        string stepId,
        ComposableTaskStep step,
        List<ComposableTaskStep> allSteps,
        HashSet<string> visited,
        HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(stepId))
            return true;

        if (visited.Contains(stepId))
            return false;

        visited.Add(stepId);
        recursionStack.Add(stepId);

        var stepMap = allSteps.ToDictionary(s => s.Id);
        foreach (var dep in step.DependsOn)
        {
            if (stepMap.TryGetValue(dep, out var depStep))
            {
                if (HasCircularDependency(dep, depStep, allSteps, visited, recursionStack))
                    return true;
            }
        }

        recursionStack.Remove(stepId);
        return false;
    }
}
