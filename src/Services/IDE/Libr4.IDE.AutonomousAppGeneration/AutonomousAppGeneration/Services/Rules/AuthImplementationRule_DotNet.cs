using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.Rules;

/// <summary>
/// Roslyn-backed replacement for the substring-based auth check
/// (<see cref="AutonomousQualityGateService"/>'s <c>HasAuthImplementationSignals</c>).
///
/// Pass criteria — at least one of:
///   * a call expression containing "AddAuthentication" / "AddJwtBearer";
///   * a method/class decorated with [Authorize];
///   * an invocation of UseAuthentication / UseAuthorization on the app pipeline;
///   * registered IdentityUser / configured cookie auth scheme.
///
/// Comments and READMEs are NOT enough — those are intentionally rejected because
/// the legacy substring rule produced false positives on docs.
/// </summary>
public sealed class AuthImplementationRule_DotNet : IArchitectureCheckRule
{
    public string CheckId => "auth_implementation";

    public bool AppliesTo(GenerationPlan plan) => StackPlanHeuristics.IsAspNetCore(plan);

    public Task<ArchitectureCheckOutcome> EvaluateAsync(
        IReadOnlyList<GeneratedFile> files,
        GenerationPlan plan,
        CancellationToken ct)
    {
        var evidence = new List<string>();

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            if (!file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(file.Content))
                continue;

            var tree = CSharpSyntaxTree.ParseText(file.Content!, cancellationToken: ct);
            var root = tree.GetRoot(ct);

            // 1. Pipeline registrations: AddAuthentication / AddJwtBearer / UseAuthentication / UseAuthorization
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var name = ExtractInvocationName(invocation);
                if (name is null) continue;
                if (IsAuthApi(name))
                {
                    evidence.Add($"{file.RelativePath}:{name}");
                    if (evidence.Count >= 4) break;
                }
            }

            if (evidence.Count >= 4) break;

            // 2. [Authorize] attributes on methods or classes (resilience: no using import required).
            foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
            {
                var attrName = (attribute.Name as IdentifierNameSyntax)?.Identifier.Text
                    ?? (attribute.Name as QualifiedNameSyntax)?.Right.Identifier.Text;
                if (attrName == "Authorize" || attrName == "AuthorizeAttribute")
                {
                    evidence.Add($"{file.RelativePath}:Authorize-attr");
                    if (evidence.Count >= 4) break;
                }
            }

            if (evidence.Count >= 4) break;
        }

        var satisfied = evidence.Count > 0;
        return Task.FromResult(new ArchitectureCheckOutcome(
            CheckId,
            satisfied,
            satisfied ? $"auth_signals: {string.Join(", ", evidence.Take(4))}" : null,
            satisfied ? null : "Wire authentication into the pipeline (services.AddAuthentication().AddJwtBearer(...) + app.UseAuthentication()).",
            evidence));
    }

    private static string? ExtractInvocationName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            IdentifierNameSyntax id => id.Identifier.Text,
            _ => null
        };
    }

    private static bool IsAuthApi(string name)
    {
        return name.Equals("AddAuthentication", StringComparison.Ordinal)
            || name.Equals("AddJwtBearer", StringComparison.Ordinal)
            || name.Equals("AddIdentity", StringComparison.Ordinal)
            || name.Equals("AddIdentityCore", StringComparison.Ordinal)
            || name.Equals("UseAuthentication", StringComparison.Ordinal)
            || name.Equals("UseAuthorization", StringComparison.Ordinal)
            || name.Equals("AddAuthorization", StringComparison.Ordinal);
    }
}
