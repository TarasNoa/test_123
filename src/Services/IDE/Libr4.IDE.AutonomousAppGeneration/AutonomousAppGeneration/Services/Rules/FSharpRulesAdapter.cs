using Libr4.IDE.Domain.AutonomousAppGeneration;
using Libr4.IDE.AutonomousAppGeneration.Rules;
using Microsoft.FSharp.Collections;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;

/// <summary>
/// P2-7 of audit roadmap.
/// Bridges the F# ReviewGate2 rules engine to the C# <see cref="IArchitectureCheckRule"/> contract.
/// Calls F# <c>ReviewGate2.evaluateAll</c> and surfaces only the outcome for <see cref="CheckId"/>.
/// </summary>
public sealed class FSharpRulesAdapter : IArchitectureCheckRule
{
    private readonly string _checkId;
    private readonly string _defaultStackTag;

    public FSharpRulesAdapter(string checkId, string defaultStackTag = "any")
    {
        _checkId = checkId;
        _defaultStackTag = defaultStackTag;
    }

    public string CheckId => _checkId;

    public bool AppliesTo(GenerationPlan plan) => true;

    public Task<ArchitectureCheckOutcome> EvaluateAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        CancellationToken ct)
    {
        // Build F# list from C# collection via ListModule helper.
        var fsFiles = ListModule.OfSeq(files.Select(ToFsFile));

        var stackTag = InferStackTag(plan) ?? _defaultStackTag;
        var results = ReviewGate2.evaluateAll(stackTag, fsFiles);

        // results is FSharpList<(string * bool * string)> — reason is "" when absent
        foreach (var tuple in results)
        {
            var (id, satisfied, reason) = tuple;
            if (!string.Equals(id, _checkId, StringComparison.OrdinalIgnoreCase))
                continue;

            return Task.FromResult(new ArchitectureCheckOutcome(
                CheckId: _checkId,
                Satisfied: satisfied,
                Detail: string.IsNullOrEmpty(reason) ? null : reason));
        }

        return Task.FromResult(new ArchitectureCheckOutcome(_checkId, Satisfied: true, Detail: "not_applicable"));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Libr4.IDE.AutonomousAppGeneration.Rules.Domain.FileInput ToFsFile(GeneratedFile f) =>
        new Libr4.IDE.AutonomousAppGeneration.Rules.Domain.FileInput(f.RelativePath, f.Content ?? string.Empty);

    private static string? InferStackTag(GenerationPlan plan)
    {
        if (plan.TechStack.Languages.Any(l =>
                l.Equals("python", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("python", StringComparison.OrdinalIgnoreCase)))
            return "python";

        if (plan.TechStack.Languages.Any(l =>
                l.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
                l.Equals("c#", StringComparison.OrdinalIgnoreCase)))
            return "dotnet";

        if (plan.TechStack.Languages.Any(l =>
                l.Equals("javascript", StringComparison.OrdinalIgnoreCase) ||
                l.Equals("typescript", StringComparison.OrdinalIgnoreCase)))
            return "node";

        return null;
    }
}
