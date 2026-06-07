using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Reasoning;

public sealed record ReasoningParseResult(string VisibleContent, string? ReasoningContent);

public static class ReasoningChannelParser
{
    public static ReasoningParseResult Split(string raw) =>
        FSharpAlgorithmsBridge.SplitReasoningChannel(raw);
}
