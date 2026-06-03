using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Validation result for a plan's build/test command list.
/// </summary>
public sealed record PlanCommandValidationResult(
    bool IsValid,
    IReadOnlyList<string> Issues,
    IReadOnlyList<string> SafeReplacements);

/// <summary>
/// P1-10 of audit roadmap. Catches the common class of plan failures observed in
/// the legacy ENHANCED_GENERATION_TEST_RESULTS run where build commands were
/// truncated/malformed (e.g. <c>"restore'"</c>) and the orchestrator burned 8 iterations
/// trying to fix code instead of the plan.
///
/// Rules (deterministic, no shell exec):
///   - Empty / whitespace command rejected.
///   - Unmatched single/double quotes rejected.
///   - Trailing/leading shell-control character rejected (`;`, `&`, `|` at boundaries).
///   - Backtick / $() injection rejected (sandbox safety).
///   - Provides safe per-stack defaults (`dotnet restore && dotnet build` etc).
/// </summary>
public interface IPlanCommandValidator
{
    PlanCommandValidationResult Validate(GenerationPlan plan);

    /// <summary>Normalizes fixable monorepo commands, then throws if still invalid.</summary>
    GenerationPlan EnsureValidOrThrow(GenerationPlan plan);

    /// <summary>Suggest known-good build/test commands for the detected stack.</summary>
    (IReadOnlyList<string> Build, IReadOnlyList<string> Test) GetSafeDefaults(GenerationPlan plan);
}

public sealed class DefaultPlanCommandValidator : IPlanCommandValidator
{
    public PlanCommandValidationResult Validate(GenerationPlan plan)
    {
        var issues = new List<string>();
        var replacements = new List<string>();

        ValidateGroup(plan.BuildCommands, "build", issues);
        ValidateGroup(plan.TestCommands, "test", issues);

        if (plan.BuildCommands.Count == 0)
        {
            issues.Add("missing_build_commands");
            replacements.Add("plan.BuildCommands_was_empty -> use stack defaults");
        }
        if (plan.TestCommands.Count == 0)
        {
            issues.Add("missing_test_commands");
            replacements.Add("plan.TestCommands_was_empty -> use stack defaults");
        }

        ValidateJavaReactMonorepoLayout(plan, issues);

        return new PlanCommandValidationResult(issues.Count == 0, issues, replacements);
    }

    public GenerationPlan EnsureValidOrThrow(GenerationPlan plan)
    {
        var normalized = NormalizeJavaReactMonorepoCommands(plan);
        var validation = Validate(normalized);
        if (validation.IsValid)
            return normalized;

        throw new AutonomousGenerationFailedException(
            "plan_command_validation",
            $"Plan build/test commands are invalid: {string.Join(", ", validation.Issues)}");
    }

    public (IReadOnlyList<string> Build, IReadOnlyList<string> Test) GetSafeDefaults(GenerationPlan plan)
    {
        return Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackPlanHeuristics.Classify(plan) switch
        {
            Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.DotNet =>
                (new[] { "dotnet restore", "dotnet build --configuration Release" },
                 new[] { "dotnet test --configuration Release" }),

            Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.Python =>
                (new[] { "pip install -r requirements.txt" },
                 new[] { "pytest" }),

            Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.Node =>
                (new[] { "npm ci", "npm run build" },
                 new[] { "npm test" }),

            Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.JavaReactFullStack =>
                (new[]
                    {
                        "export DEBIAN_FRONTEND=noninteractive && apt-get update -qq && apt-get install -y -qq maven npm > /dev/null",
                        "cd backend && mvn -q -DskipTests package",
                        "cd frontend && npm ci && npm run build"
                    },
                 new[]
                    {
                        "export DEBIAN_FRONTEND=noninteractive && apt-get update -qq && apt-get install -y -qq maven npm > /dev/null",
                        "cd backend && mvn -q test",
                        "cd frontend && npm test -- --watch=false"
                    }),

            Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.Java =>
                (new[] { "cd backend && mvn -q -DskipTests package" },
                 new[] { "cd backend && mvn -q test" }),

            _ => (new[] { "echo no_build_command_configured" },
                  new[] { "echo no_test_command_configured" })
        };
    }

    private static GenerationPlan NormalizeJavaReactMonorepoCommands(GenerationPlan plan)
    {
        if (Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackPlanHeuristics.Classify(plan)
            != Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.JavaReactFullStack)
            return plan;

        static string NormalizeCommand(string cmd, bool backend)
        {
            if (string.IsNullOrWhiteSpace(cmd)) return cmd;
            var trimmed = cmd.Trim();
            if (backend)
            {
                if (trimmed.Contains("cd backend", StringComparison.OrdinalIgnoreCase)) return trimmed;
                if (trimmed.Contains("mvn", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("mvnw", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("gradle", StringComparison.OrdinalIgnoreCase))
                    return $"cd backend && {trimmed}";
            }
            else
            {
                if (trimmed.Contains("cd frontend", StringComparison.OrdinalIgnoreCase)) return trimmed;
                if (trimmed.Contains("npm", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("yarn", StringComparison.OrdinalIgnoreCase)
                    || trimmed.Contains("pnpm", StringComparison.OrdinalIgnoreCase))
                    return $"cd frontend && {trimmed}";
            }

            return trimmed;
        }

        var build = plan.BuildCommands
            .Select(c =>
                c.Contains("mvn", StringComparison.OrdinalIgnoreCase)
                || c.Contains("mvnw", StringComparison.OrdinalIgnoreCase)
                || c.Contains("gradle", StringComparison.OrdinalIgnoreCase)
                    ? NormalizeCommand(c, backend: true)
                    : (c.Contains("npm", StringComparison.OrdinalIgnoreCase)
                       || c.Contains("yarn", StringComparison.OrdinalIgnoreCase)
                       || c.Contains("pnpm", StringComparison.OrdinalIgnoreCase)
                        ? NormalizeCommand(c, backend: false)
                        : c))
            .ToList();

        var test = plan.TestCommands
            .Select(c =>
                c.Contains("mvn", StringComparison.OrdinalIgnoreCase)
                || c.Contains("mvnw", StringComparison.OrdinalIgnoreCase)
                    ? NormalizeCommand(c, backend: true)
                    : (c.Contains("npm", StringComparison.OrdinalIgnoreCase)
                       || c.Contains("yarn", StringComparison.OrdinalIgnoreCase)
                       || c.Contains("pnpm", StringComparison.OrdinalIgnoreCase)
                        ? NormalizeCommand(c, backend: false)
                        : c))
            .ToList();

        return new GenerationPlan(
            plan.ApplicationName,
            plan.ApplicationDescription,
            plan.TechStack,
            plan.Phases,
            plan.RequiredAgents,
            plan.RuntimeImage,
            build,
            test,
            plan.MaxIterations);
    }

    private static void ValidateJavaReactMonorepoLayout(GenerationPlan plan, List<string> issues)
    {
        if (Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackPlanHeuristics.Classify(plan)
            != Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure.StackKind.JavaReactFullStack)
            return;

        foreach (var cmd in plan.BuildCommands)
        {
            if (cmd.Contains("mvn", StringComparison.OrdinalIgnoreCase)
                && !cmd.Contains("cd backend", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("java_react_build_missing_backend_cd");
            }
            if ((cmd.Contains("npm", StringComparison.OrdinalIgnoreCase)
                 || cmd.Contains("yarn", StringComparison.OrdinalIgnoreCase))
                && !cmd.Contains("cd frontend", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add("java_react_build_missing_frontend_cd");
            }
        }
    }

    private static void ValidateGroup(IReadOnlyList<string> commands, string label, List<string> issues)
    {
        for (var i = 0; i < commands.Count; i++)
        {
            var raw = commands[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                issues.Add($"{label}_cmd_{i}:empty");
                continue;
            }
            var trimmed = raw.Trim();

            if (HasUnbalancedQuotes(trimmed))
            {
                issues.Add($"{label}_cmd_{i}:unbalanced_quotes");
            }

            if (StartsOrEndsWithControlChar(trimmed))
            {
                issues.Add($"{label}_cmd_{i}:control_char_at_boundary");
            }

            if (ContainsCommandSubstitution(trimmed))
            {
                issues.Add($"{label}_cmd_{i}:command_substitution_disallowed");
            }

            if (trimmed.Length > 1000)
            {
                issues.Add($"{label}_cmd_{i}:exceeds_max_length");
            }
        }
    }

    private static bool HasUnbalancedQuotes(string s)
    {
        var single = 0;
        var dbl = 0;
        var escape = false;
        foreach (var c in s)
        {
            if (escape) { escape = false; continue; }
            if (c == '\\') { escape = true; continue; }
            if (c == '\'') single++;
            else if (c == '"') dbl++;
        }
        return single % 2 != 0 || dbl % 2 != 0;
    }

    private static bool StartsOrEndsWithControlChar(string s)
    {
        if (s.Length == 0) return true;
        ReadOnlySpan<char> control = stackalloc char[] { '|', '&', ';', '<', '>' };
        return control.IndexOf(s[0]) >= 0 || control.IndexOf(s[^1]) >= 0;
    }

    private static bool ContainsCommandSubstitution(string s)
    {
        // Disallow $(...) and `...` substitutions which can be used for sandbox escape.
        // Allow plain $VARS because environment expansion is needed.
        if (s.Contains("$(", StringComparison.Ordinal)) return true;
        if (s.Contains('`')) return true;
        return false;
    }
}
