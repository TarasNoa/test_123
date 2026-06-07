using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.Algorithms;
using Libr4.IDE.AutonomousAppGeneration.Algorithms.FastContext;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.FastContext;

public sealed class FastContextFusionRanker
{
    private readonly FastContextOptions _options;

    public FastContextFusionRanker(IOptions<FastContextOptions> options) => _options = options.Value;

    public IReadOnlyList<CodebaseSearchHit> Fuse(
        IReadOnlyList<CodebaseSearchHit> ripgrepHits,
        IReadOnlyList<(CodebaseSearchHit Hit, double Boost)> graphBoosts,
        int limit)
    {
        var fusionOptions = new FusionRanker.FusionOptions(
            _options.RrfK,
            _options.RipgrepWeight,
            _options.GraphWeight,
            _options.PathHeuristicWeight);

        var rg = ripgrepHits.Select(FSharpAlgorithmsBridge.ToFSharpHit).ToArray();
        var boosts = graphBoosts.Select(b => (FSharpAlgorithmsBridge.ToFSharpHit(b.Hit), b.Boost)).ToArray();
        var fused = FSharpAlgorithmsBridge.FuseSearchHits(rg, boosts, limit, fusionOptions);

        return fused
            .Select(h => new CodebaseSearchHit(
                h.Path,
                h.StartLine,
                h.StartLine,
                h.Score,
                h.Snippet,
                h.MatchKind))
            .ToList();
    }
}
