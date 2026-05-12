using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Libr4.IDE.Domain.FSharp;

namespace Libr4.IDE.Infrastructure.FSharpInterop;

/// <summary>
/// C# Bridge for F# Agent State Machine
/// Provides idiomatic C# API over F# discriminated unions
/// </summary>
public interface IAgentStateMachineBridge
{
    /// <summary>
    /// Create new agent in Idle state
    /// </summary>
    FSharpAgentState CreateIdleState(string agentId, string[] capabilities);

    /// <summary>
    /// Initialize agent with context
    /// </summary>
    FSharpAgentState Initialize(FSharpAgentState currentState, Dictionary<string, object> context);

    /// <summary>
    /// Mark agent as ready
    /// </summary>
    FSharpAgentState MarkReady(FSharpAgentState currentState, string[] availableTools);

    /// <summary>
    /// Start thinking phase
    /// </summary>
    FSharpAgentState StartThinking(FSharpAgentState currentState, AgentTaskBridge task);

    /// <summary>
    /// Start execution phase
    /// </summary>
    FSharpAgentState StartExecuting(FSharpAgentState currentState, string[] subtasks);

    /// <summary>
    /// Get current state name
    /// </summary>
    string GetStateName(FSharpAgentState state);

    /// <summary>
    /// Get agent ID from any state
    /// </summary>
    string GetAgentId(FSharpAgentState state);

    /// <summary>
    /// Check if agent can accept new task
    /// </summary>
    bool CanAcceptTask(FSharpAgentState state);

    /// <summary>
    /// Check if agent is active (not terminal)
    /// </summary>
    bool IsActive(FSharpAgentState state);

    /// <summary>
    /// Get progress percentage (0.0 to 1.0)
    /// </summary>
    double GetProgress(FSharpAgentState state);

    /// <summary>
    /// Update subtask progress
    /// </summary>
    FSharpAgentState UpdateSubtask(FSharpAgentState currentState, string subtaskId, SubtaskStatus status);

    /// <summary>
    /// Complete validation phase
    /// </summary>
    FSharpAgentState CompleteValidation(FSharpAgentState currentState, AgentResultBridge result, ValidationRuleBridge[] rules);

    /// <summary>
    /// Fail agent with error
    /// </summary>
    FSharpAgentState Fail(FSharpAgentState currentState, AgentErrorBridge error, RecoveryStrategy strategy);

    /// <summary>
    /// Dispose agent
    /// </summary>
    FSharpAgentState Dispose(FSharpAgentState state);
}

/// <summary>
/// F# state wrapper for C#
/// </summary>
public record FSharpAgentState(object InternalState);

public enum AgentTaskPriority { Low, Normal, High, Critical }

/// <summary>
/// Agent task for C#
/// </summary>
public record AgentTaskBridge(
    string TaskId,
    string TaskType,
    string Description,
    AgentTaskPriority Priority,
    TimeSpan? Deadline,
    Dictionary<string, object> Context);

/// <summary>
/// Agent result for C#
/// </summary>
public record AgentResultBridge(
    string ResultId,
    string Content,
    object[] Artifacts,
    Dictionary<string, double> Metrics,
    string[] Warnings);

/// <summary>
/// Validation rule for C#
/// </summary>
public record ValidationRuleBridge(string RuleName, string ErrorMessage);

/// <summary>
/// Agent error for C#
/// </summary>
public record AgentErrorBridge(AgentErrorType Type, string Message, string? Details);

/// <summary>
/// Recovery strategy
/// </summary>
public enum RecoveryStrategy
{
    Retry3,
    Retry1,
    Fallback,
    Escalate,
    Terminate
}

/// <summary>
/// Agent error types
/// </summary>
public enum AgentErrorType
{
    Validation,
    Execution,
    Timeout,
    ConsensusFailed,
    SecurityViolation,
    ResourceExhausted
}

/// <summary>
/// Subtask status
/// </summary>
public enum SubtaskStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

/// <summary>
/// Implementation
/// </summary>
public class AgentStateMachineBridge : IAgentStateMachineBridge
{
    private readonly ILogger<AgentStateMachineBridge> _logger;

    public AgentStateMachineBridge(ILogger<AgentStateMachineBridge> logger)
    {
        _logger = logger;
    }

    public FSharpAgentState CreateIdleState(string agentId, string[] capabilities)
    {
        try
        {
            var state = AgentCSharpInterop.createIdleStateForCSharp(agentId, capabilities);
            
            _logger.LogDebug("Created idle state for agent {AgentId}", agentId);
            return new FSharpAgentState(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create idle state for agent {AgentId}", agentId);
            throw;
        }
    }

    public FSharpAgentState Initialize(FSharpAgentState currentState, Dictionary<string, object> context)
    {
        try
        {
            var fsharpContext = ConvertToFSharpMap(context);
            
            if (currentState.InternalState is AgentState.Idle idle)
            {
                var newState = AgentStateMachine.initialize(idle, fsharpContext);
                return new FSharpAgentState(newState);
            }
            
            throw new InvalidOperationException($"Cannot initialize from state: {GetStateName(currentState)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize agent");
            throw;
        }
    }

    public FSharpAgentState MarkReady(FSharpAgentState currentState, string[] availableTools)
    {
        try
        {
            var fsharpTools = ListModule.OfSeq(availableTools);
            
            if (currentState.InternalState is AgentState.Initializing init)
            {
                var newState = AgentStateMachine.markReady(init, fsharpTools);
                return new FSharpAgentState(newState);
            }
            
            throw new InvalidOperationException($"Cannot mark ready from state: {GetStateName(currentState)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark agent ready");
            throw;
        }
    }

    public FSharpAgentState StartThinking(FSharpAgentState currentState, AgentTaskBridge task)
    {
        try
        {
            var fsharpTask = ConvertToFSharpTask(task);
            
            if (currentState.InternalState is AgentState.Ready ready)
            {
                var newState = AgentStateMachine.startThinking(ready, fsharpTask);
                return new FSharpAgentState(newState);
            }
            
            throw new InvalidOperationException($"Cannot start thinking from state: {GetStateName(currentState)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start thinking phase");
            throw;
        }
    }

    public FSharpAgentState StartExecuting(FSharpAgentState currentState, string[] subtasks)
    {
        try
        {
            if (currentState.InternalState is AgentState.Thinking thinking)
            {
                // Convert subtask strings to F# list
                var fsharpSubtasks = ListModule.OfSeq(subtasks);
                var newState = AgentStateMachine.startExecuting(thinking, fsharpSubtasks);
                return new FSharpAgentState(newState);
            }
            
            throw new InvalidOperationException($"Cannot start executing from state: {GetStateName(currentState)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start execution phase");
            throw;
        }
    }

    public string GetStateName(FSharpAgentState state)
    {
        try
        {
            if (state.InternalState is AgentState s) return AgentCSharpInterop.getStateName(s);
            return "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get state name");
            return "Unknown";
        }
    }

    public string GetAgentId(FSharpAgentState state)
    {
        try
        {
            if (state.InternalState is AgentState sa) return AgentStateMachine.getAgentId(sa);
            return "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent ID");
            return "unknown";
        }
    }

    public bool CanAcceptTask(FSharpAgentState state)
    {
        try
        {
            return AgentCSharpInterop.canAcceptTaskForCSharp(state.InternalState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if can accept task");
            return false;
        }
    }

    public bool IsActive(FSharpAgentState state)
    {
        try
        {
            if (state.InternalState is AgentState si) return AgentStateMachine.isActive(si);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if active");
            return false;
        }
    }

    public double GetProgress(FSharpAgentState state)
    {
        try
        {
            return AgentCSharpInterop.getProgressForCSharp(state.InternalState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get progress");
            return 0.0;
        }
    }

    public FSharpAgentState UpdateSubtask(FSharpAgentState currentState, string subtaskId, SubtaskStatus status)
    {
        try
        {
            if (currentState.InternalState is AgentState.Executing executing)
            {
                var fsharpStatus = ConvertToFSharpSubtaskStatus(status);
                var updatedExecuting = AgentStateMachine.updateSubtask(executing, subtaskId, fsharpStatus);
                var newState = AgentState.NewExecuting(updatedExecuting);
                return new FSharpAgentState(newState);
            }
            
            throw new InvalidOperationException($"Cannot update subtask from state: {GetStateName(currentState)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update subtask");
            throw;
        }
    }

    public FSharpAgentState CompleteValidation(FSharpAgentState currentState, AgentResultBridge result, ValidationRuleBridge[] rules)
    {
        try
        {
            if (currentState.InternalState is AgentState.Validating validating)
            {
                var fsharpResult = ConvertToFSharpResult(result);
                var fsharpRules = ListModule.OfSeq(Array.ConvertAll(rules, ConvertToFSharpRule));
                
                var newState = AgentStateMachine.completeDirect(validating);
                return new FSharpAgentState(newState);
            }
            
            throw new InvalidOperationException($"Cannot complete validation from state: {GetStateName(currentState)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete validation");
            throw;
        }
    }

    public FSharpAgentState Fail(FSharpAgentState currentState, AgentErrorBridge error, RecoveryStrategy strategy)
    {
        try
        {
            var fsharpError = ConvertToFSharpError(error);
            var fsharpRecovery = ConvertToFSharpRecovery(strategy);
            
            if (currentState.InternalState is AgentState sf)
            {
                var newState = AgentStateMachine.fail(sf, fsharpError, fsharpRecovery);
                return new FSharpAgentState(newState);
            }
            throw new InvalidOperationException("Cannot fail from non-agent state");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transition to failed state");
            throw;
        }
    }

    public FSharpAgentState Dispose(FSharpAgentState state)
    {
        try
        {
            if (state.InternalState is AgentState sd)
            {
                var newState = AgentStateMachine.dispose(sd);
                return new FSharpAgentState(newState);
            }
            throw new InvalidOperationException("Cannot dispose non-agent state");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispose agent");
            throw;
        }
    }

    // Conversion helpers
    private static FSharpMap<string, object> ConvertToFSharpMap(Dictionary<string, object> dict)
    {
        var list = new List<Tuple<string, object>>();
        foreach (var kvp in dict)
        {
            list.Add(Tuple.Create(kvp.Key, kvp.Value));
        }
        return MapModule.OfSeq(list);
    }

    private static AgentTask ConvertToFsharpTask(AgentTaskBridge task)
    {
        return new AgentTask(
            task.TaskId,
            task.TaskType,
            task.Description,
            ConvertToFsharpPriority(task.Priority),
            task.Deadline.HasValue ? FSharpOption<TimeSpan>.Some(task.Deadline.Value) : FSharpOption<TimeSpan>.None,
            ConvertToFSharpMap(task.Context));
    }

    private static TaskPriority ConvertToFsharpPriority(AgentTaskPriority priority)
    {
        return priority switch
        {
            AgentTaskPriority.Critical => TaskPriority.Critical,
            AgentTaskPriority.High => TaskPriority.High,
            AgentTaskPriority.Normal => TaskPriority.Normal,
            _ => TaskPriority.Low
        };
    }

    private static AgentResult ConvertToFsharpResult(AgentResultBridge result)
    {
        return new AgentResult(
            result.ResultId,
            result.Content,
            ListModule.OfSeq(result.Artifacts),
            ConvertToFSharpMap(result.Metrics),
            ListModule.OfSeq(result.Warnings));
    }

    private static ValidationRule ConvertToFsharpRule(ValidationRuleBridge rule)
    {
        return new ValidationRule(rule.RuleName, _ => true, rule.ErrorMessage);
    }

    private static AgentError ConvertToFsharpError(AgentErrorBridge error)
    {
        return error.Type switch
        {
            AgentErrorType.Validation => AgentError.NewValidationError(error.Message),
            AgentErrorType.Execution => AgentError.NewExecutionError(error.Message, new Exception(error.Details)),
            AgentErrorType.Timeout => AgentError.NewTimeoutError(TimeSpan.FromMinutes(5)),
            AgentErrorType.ConsensusFailed => AgentError.NewConsensusFailed(error.Message),
            AgentErrorType.SecurityViolation => AgentError.NewSecurityViolation(error.Message),
            AgentErrorType.ResourceExhausted => AgentError.NewResourceExhausted(error.Message),
            _ => AgentError.NewValidationError(error.Message)
        };
    }

    private static RecoveryOption ConvertToFsharpRecovery(RecoveryStrategy strategy)
    {
        return strategy switch
        {
            RecoveryStrategy.Retry3 => RecoveryOption.NewRetry(3),
            RecoveryStrategy.Retry1 => RecoveryOption.NewRetry(1),
            RecoveryStrategy.Fallback => RecoveryOption.NewFallback("fallback-agent"),
            RecoveryStrategy.Escalate => RecoveryOption.NewEscalate("human-operator"),
            RecoveryStrategy.Terminate => RecoveryOption.NewTerminate(),
            _ => RecoveryOption.NewTerminate()
        };
    }

    private static SubtaskState ConvertToFSharpSubtaskStatus(SubtaskStatus status)
    {
        return status switch
        {
            SubtaskStatus.Pending => SubtaskState.NewPending("subtask"),
            SubtaskStatus.InProgress => SubtaskState.NewInProgress(new InProgressData("id", DateTime.UtcNow, FSharpOption<string>.None, 0.0)),
            SubtaskStatus.Completed => SubtaskState.NewCompleted(new object()),
            SubtaskStatus.Failed => SubtaskState.NewFailed(AgentError.NewValidationError("Failed")),
            _ => SubtaskState.NewPending("subtask")
        };
    }
}

/// <summary>
/// Extension methods for easier F# interop
/// </summary>
public static class AgentStateExtensions
{
    public static string GetStateName(this FSharpAgentState state)
    {
        // Would use reflection or cached method
        return state.InternalState?.GetType().Name ?? "Unknown";
    }

    public static bool IsTerminal(this FSharpAgentState state)
    {
        var name = state.GetStateName();
        return name.Contains("Completed") || name.Contains("Failed") || name.Contains("Disposed");
    }

    public static string GetSummary(this FSharpAgentState state)
    {
        return $"AgentState[{state.GetStateName()}]";
    }
}
