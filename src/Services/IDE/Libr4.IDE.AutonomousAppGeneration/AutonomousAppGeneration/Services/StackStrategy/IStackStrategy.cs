using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.StackStrategy;

/// <summary>
/// P1-9 of audit roadmap. Single-source-of-truth strategy per tech stack.
/// Encapsulates language detection, default commands, runtime image, and
/// stack-specific test paths so the orchestrator no longer has 5 copies of
/// <c>IsAspNetCorePlan</c> / <c>IsPython</c> / <c>IsNode</c>.
///
/// Resolution: <see cref="IStackStrategyResolver"/>.<see cref="IStackStrategyResolver.Resolve"/>
/// returns a single strategy in priority order; falling back to <see cref="UnknownStackStrategy"/>.
/// </summary>
public interface IStackStrategy
{
    string StackId { get; }
    StackKind Kind { get; }
    bool Matches(GenerationPlan plan);

    string PreferredRuntimeImage { get; }
    IReadOnlyList<string> DefaultBuildCommands { get; }
    IReadOnlyList<string> DefaultTestCommands { get; }

    /// <summary>True if the path looks like an idiomatic test file for this stack.</summary>
    bool IsTestPath(string relativePath);
}

public interface IStackStrategyResolver
{
    /// <summary>Resolves the strategy for a given plan. Never returns null.</summary>
    IStackStrategy Resolve(GenerationPlan plan);

    /// <summary>All registered strategies (including unknown fallback).</summary>
    IReadOnlyList<IStackStrategy> All { get; }
}

public sealed class StackStrategyResolver : IStackStrategyResolver
{
    private readonly IReadOnlyList<IStackStrategy> _strategies;
    private readonly IStackStrategy _fallback;

    public StackStrategyResolver(IEnumerable<IStackStrategy> strategies)
    {
        _strategies = strategies.OrderBy(s => StackOrder(s.Kind)).ToList();
        _fallback = _strategies.FirstOrDefault(s => s.Kind == StackKind.Unknown)
                    ?? new UnknownStackStrategy();
    }

    public IReadOnlyList<IStackStrategy> All => _strategies;

    public IStackStrategy Resolve(GenerationPlan plan)
    {
        foreach (var s in _strategies)
        {
            if (s.Kind == StackKind.Unknown) continue;
            if (s.Matches(plan)) return s;
        }
        return _fallback;
    }

    // Resolution order: Python > Node > DotNet > Unknown.
    // Mirrors StackPlanHeuristics.IsAspNetCore which excludes python+node first.
    private static int StackOrder(StackKind kind) => kind switch
    {
        StackKind.Python => 1,
        StackKind.Node => 2,
        StackKind.DotNet => 3,
        _ => 99,
    };
}
