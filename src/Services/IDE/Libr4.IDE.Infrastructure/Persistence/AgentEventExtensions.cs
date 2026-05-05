using Libr4.IDE.Application.AutonomousAppGeneration.AgentEvents;
using Libr4.IDE.Domain.FSharp;

namespace Libr4.IDE.Infrastructure.Persistence;

/// <summary>
/// Extension methods to convert between AgentEvent domain and F# state
/// </summary>
public static class AgentEventExtensions
{
    /// <summary>
    /// Convert AgentEvent to F# AgentState
    /// </summary>
    public static AgentState ToFSharpState(this AgentEvent? agentEvent)
    {
        if (agentEvent == null)
            return AgentState.Idle;

        // Map AgentEventType to F# state
        return agentEvent.Type switch
        {
            "Idle" => AgentState.Idle,
            "Processing" or "Busy" => AgentState.Processing(agentEvent.RunId),
            "Validating" => AgentState.Validating(agentEvent.Output ?? ""),
            "Error" or "Failed" => AgentState.Failed(agentEvent.Output ?? "Unknown error"),
            _ => AgentState.Idle
        };
    }

    /// <summary>
    /// Convert F# AgentState to string for persistence
    /// </summary>
    public static string ToStateString(this AgentState state)
    {
        return state switch
        {
            AgentState.Idle => "Idle",
            AgentState.Processing id => $"Processing({id})",
            AgentState.Validating res => $"Validating({res})",
            AgentState.Failed reason => $"Failed({reason})",
            _ => "Unknown"
        };
    }
}
