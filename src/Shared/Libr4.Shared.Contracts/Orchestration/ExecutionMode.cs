namespace Libr4.Shared.Contracts.Orchestration;

/// <summary>
/// Execution mode for agent orchestration.
/// Defines how the agent interacts with the user and executes tasks.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Copilot mode: User-driven, agent provides suggestions and assistance.
    /// The user is in control and the agent acts as an intelligent assistant.
    /// </summary>
    Copilot,

    /// <summary>
    /// Agent mode: Autonomous execution with minimal user intervention.
    /// The agent takes the lead and executes tasks independently.
    /// </summary>
    Agent,

    /// <summary>
    /// Flow mode: Dynamic switching between Copilot and Agent based on context.
    /// Automatically adjusts the level of autonomy based on task complexity, context length, and user preferences.
    /// </summary>
    Flow
}

/// <summary>
/// Context information for mode switching decisions.
/// </summary>
public record ModeSwitchContext
{
    /// <summary>
    /// Current execution mode.
    /// </summary>
    public ExecutionMode CurrentMode { get; init; }

    /// <summary>
    /// Current context length in tokens.
    /// </summary>
    public int ContextLength { get; init; }

    /// <summary>
    /// Maximum context length in tokens.
    /// </summary>
    public int MaxContextLength { get; init; }

    /// <summary>
    /// Task complexity score (0-1).
    /// </summary>
    public double TaskComplexity { get; init; }

    /// <summary>
    /// Number of steps completed.
    /// </summary>
    public int StepsCompleted { get; init; }

    /// <summary>
    /// Total estimated steps.
    /// </summary>
    public int TotalSteps { get; init; }

    /// <summary>
    /// Whether the user has requested more autonomy.
    /// </summary>
    public bool UserRequestedAutonomy { get; init; }

    /// <summary>
    /// Whether the user has requested more control.
    /// </summary>
    public bool UserRequestedControl { get; init; }

    /// <summary>
    /// Error rate in recent steps.
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Additional metadata for mode switching.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Result of a mode switch operation.
/// </summary>
public record ModeSwitchResult
{
    /// <summary>
    /// Whether the mode was switched.
    /// </summary>
    public bool Switched { get; init; }

    /// <summary>
    /// Previous mode.
    /// </summary>
    public ExecutionMode PreviousMode { get; init; }

    /// <summary>
    /// New mode.
    /// </summary>
    public ExecutionMode NewMode { get; init; }

    /// <summary>
    /// Reason for the mode switch.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Whether context was preserved during the switch.
    /// </summary>
    public bool ContextPreserved { get; init; }

    /// <summary>
    /// Timestamp when the switch occurred.
    /// </summary>
    public DateTime SwitchedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for managing execution mode switching.
/// </summary>
public interface IExecutionModeManager
{
    /// <summary>
    /// Gets the current execution mode.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current execution mode.</returns>
    Task<ExecutionMode> GetCurrentModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the execution mode.
    /// </summary>
    /// <param name="mode">Mode to set.</param>
    /// <param name="reason">Reason for the mode change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the mode change.</returns>
    Task<ModeSwitchResult> SetModeAsync(
        ExecutionMode mode,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether to switch modes based on context.
    /// </summary>
    /// <param name="context">Context for mode switching decision.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recommended mode switch, or null if no switch is needed.</returns>
    Task<ModeSwitchResult?> EvaluateModeSwitchAsync(
        ModeSwitchContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preserves context when switching modes.
    /// </summary>
    /// <param name="previousMode">Previous mode.</param>
    /// <param name="newMode">New mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if context was preserved successfully.</returns>
    Task<bool> PreserveContextOnSwitchAsync(
        ExecutionMode previousMode,
        ExecutionMode newMode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of execution mode manager.
/// </summary>
public class InMemoryExecutionModeManager : IExecutionModeManager
{
    private ExecutionMode _currentMode = ExecutionMode.Flow;

    public Task<ExecutionMode> GetCurrentModeAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentMode);
    }

    public Task<ModeSwitchResult> SetModeAsync(
        ExecutionMode mode,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var previousMode = _currentMode;
        
        // Preserve context before switching
        var contextPreserved = PreserveContextOnSwitchAsync(previousMode, mode, cancellationToken)
            .GetAwaiter()
            .GetResult();

        _currentMode = mode;

        var result = new ModeSwitchResult
        {
            Switched = previousMode != mode,
            PreviousMode = previousMode,
            NewMode = mode,
            Reason = reason,
            ContextPreserved = contextPreserved,
            SwitchedAt = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    public Task<ModeSwitchResult?> EvaluateModeSwitchAsync(
        ModeSwitchContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.CurrentMode == ExecutionMode.Flow)
        {
            // Flow mode: dynamically switch based on context
            var recommendedMode = DetermineFlowMode(context);
            
            if (recommendedMode != context.CurrentMode)
            {
                var result = new ModeSwitchResult
                {
                    Switched = true,
                    PreviousMode = context.CurrentMode,
                    NewMode = recommendedMode,
                    Reason = GetSwitchReason(context, recommendedMode),
                    ContextPreserved = true
                };

                return Task.FromResult<ModeSwitchResult?>(result);
            }
        }
        else if (context.UserRequestedAutonomy && context.CurrentMode == ExecutionMode.Copilot)
        {
            // User wants more autonomy
            var result = new ModeSwitchResult
            {
                Switched = true,
                PreviousMode = context.CurrentMode,
                NewMode = ExecutionMode.Agent,
                Reason = "User requested more autonomy",
                ContextPreserved = true
            };

            return Task.FromResult<ModeSwitchResult?>(result);
        }
        else if (context.UserRequestedControl && context.CurrentMode == ExecutionMode.Agent)
        {
            // User wants more control
            var result = new ModeSwitchResult
            {
                Switched = true,
                PreviousMode = context.CurrentMode,
                NewMode = ExecutionMode.Copilot,
                Reason = "User requested more control",
                ContextPreserved = true
            };

            return Task.FromResult<ModeSwitchResult?>(result);
        }

        return Task.FromResult<ModeSwitchResult?>(null);
    }

    public Task<bool> PreserveContextOnSwitchAsync(
        ExecutionMode previousMode,
        ExecutionMode newMode,
        CancellationToken cancellationToken = default)
    {
        // In a real implementation, this would:
        // 1. Save current context state
        // 2. Transform context for the new mode
        // 3. Restore context in the new mode
        
        // For now, we assume context is always preserved
        return Task.FromResult(true);
    }

    private static ExecutionMode DetermineFlowMode(ModeSwitchContext context)
    {
        // High context utilization -> switch to Agent mode for efficiency
        if (context.ContextLength > context.MaxContextLength * 0.8)
        {
            return ExecutionMode.Agent;
        }

        // High task complexity -> switch to Copilot mode for user guidance
        if (context.TaskComplexity > 0.7)
        {
            return ExecutionMode.Copilot;
        }

        // High error rate -> switch to Copilot mode for user intervention
        if (context.ErrorRate > 0.3)
        {
            return ExecutionMode.Copilot;
        }

        // Near completion -> switch to Copilot mode for review
        if (context.TotalSteps > 0 && 
            (double)context.StepsCompleted / context.TotalSteps > 0.8)
        {
            return ExecutionMode.Copilot;
        }

        // Default: stay in Flow mode
        return ExecutionMode.Flow;
    }

    private static string GetSwitchReason(ModeSwitchContext context, ExecutionMode newMode)
    {
        if (newMode == ExecutionMode.Agent)
        {
            if (context.ContextLength > context.MaxContextLength * 0.8)
            {
                return "High context utilization - switching to Agent mode for efficiency";
            }
        }
        else if (newMode == ExecutionMode.Copilot)
        {
            if (context.TaskComplexity > 0.7)
            {
                return "High task complexity - switching to Copilot mode for user guidance";
            }
            if (context.ErrorRate > 0.3)
            {
                return "High error rate - switching to Copilot mode for user intervention";
            }
            if (context.TotalSteps > 0 && 
                (double)context.StepsCompleted / context.TotalSteps > 0.8)
            {
                return "Near completion - switching to Copilot mode for review";
            }
        }

        return "Context-driven mode switch";
    }
}
