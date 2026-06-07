using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Hermes;

public static class HermesMemoryScoring
{
    public static double ComputeRelevanceScore(HermesMemoryEntry entry, string? keyword) =>
        FSharpAlgorithmsBridge.ComputeHermesRelevanceScore(entry, keyword);

    public static string BuildRetrievalReason(HermesMemoryEntry entry, string? keyword) =>
        FSharpAlgorithmsBridge.BuildHermesRetrievalReason(entry, keyword);

    public static string KindLabel(MemoryKind kind) =>
        FSharpAlgorithmsBridge.HermesKindLabel((int)kind);
}
