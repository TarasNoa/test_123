using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public static class AgentResponseParser
{
    public static AgentTurnResponse Parse(string raw, bool stripReasoning = true) =>
        FSharpAlgorithmsBridge.ParseAgentResponse(raw, stripReasoning);
}
