using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Qdrant;

public static class ReciprocalRankFusion
{
    public const double DefaultK = 60.0;

    public static IReadOnlyList<(string Id, double Score)> Fuse(
        IReadOnlyList<IReadOnlyList<string>> rankedLists,
        double k = DefaultK) =>
        FSharpAlgorithmsBridge.FuseReciprocalRank(rankedLists, k);
}
