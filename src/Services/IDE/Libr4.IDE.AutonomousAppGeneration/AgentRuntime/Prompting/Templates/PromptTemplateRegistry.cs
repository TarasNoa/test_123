using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;

public sealed class PromptTemplateRegistry : IPromptTemplateRegistry
{
    private readonly Dictionary<string, List<PromptTemplate>> _byRoleKey;
    private readonly PromptTemplateOptions _options;
    private readonly PromptVariantSelector _variants;

    public PromptTemplateRegistry(
        IOptions<PromptTemplateOptions> options,
        PromptVariantSelector variants)
    {
        _options = options.Value;
        _variants = variants;
        _byRoleKey = BuildCatalog();
    }

    public PromptTemplate? TryGet(string role, string? variantId = null)
    {
        var key = BuildKey(role, variantId ?? _options.DefaultVariant);
        if (_byRoleKey.TryGetValue(key, out var list) && list.Count > 0)
            return list[0];

        var fallbackKey = BuildKey(role, _options.DefaultVariant);
        return _byRoleKey.TryGetValue(fallbackKey, out var fallback) && fallback.Count > 0
            ? fallback[0]
            : null;
    }

    public IReadOnlyList<PromptTemplate> ListByRole(string role) =>
        _byRoleKey
            .Where(kv => kv.Key.StartsWith(role + ":", StringComparison.OrdinalIgnoreCase))
            .SelectMany(kv => kv.Value)
            .ToList();

    public string FormatRolePrompt(string role, string? variantId = null)
    {
        if (!_options.EnableInstructionTemplates)
            return string.Empty;

        var template = TryGet(role, variantId ?? _variants.SelectVariant(role));
        if (template is null)
            return string.Empty;

        return InstructionTemplateFormatter.Format(template.InstructionBody, template.ResponseHint);
    }

    private static string BuildKey(string role, string version) =>
        $"{role.Trim().ToLowerInvariant()}:{version.Trim().ToLowerInvariant()}";

    private static Dictionary<string, List<PromptTemplate>> BuildCatalog()
    {
        var templates = new[]
        {
            new PromptTemplate("implementer-v1", "v1", "implementer",
                """
                You are the implementer subagent. Write complete production-ready files.
                Use write_file for new paths; edit_file only after read_file on existing files.
                One tool per turn. Emit done only after all target files are written.
                """,
                "Return a single JSON object: {\"action\":\"tool\",...} or {\"action\":\"done\",...}"),
            new PromptTemplate("implementer-v2", "v2", "implementer",
                """
                You are the implementer subagent (strict mode).
                Minimize exploration (max 2 read/grep/glob) then write_file with full content.
                Never omit file bodies. Match planned stack imports exactly.
                """,
                "JSON only — no markdown fences."),
            new PromptTemplate("explore-v1", "v1", "explore",
                """
                You are the explore subagent. Read-only investigation.
                Use list_directory, glob, grep, read_file. Do not mutate files.
                Summarize findings for the parent agent.
                """,
                "Return JSON tool actions or done with investigation summary."),
            new PromptTemplate("verify-v1", "v1", "verify",
                """
                You are the verify subagent. Read-only validation.
                Run tests/build, inspect outputs, report pass/fail with evidence.
                Do not edit source files.
                """,
                "Return JSON tool actions or done with verify verdict."),
            new PromptTemplate("repair-v1", "v1", "repair",
                """
                You are the repair subagent. Fix root causes from build log and errors.
                Prefer surgical edit_file/apply_patch over broad rewrites.
                Investigate CONTEXT FRAGMENTS first.
                """,
                "Return JSON tool actions or done when build/tests pass."),
            new PromptTemplate("repair-v2", "v2", "repair",
                """
                You are the repair subagent (surgical mode).
                Apply minimal diffs. run_build after each fix batch. Never guess — read logs.
                """,
                "JSON tool loop until objective met."),
            new PromptTemplate("computer-v1", "v1", "computer",
                """
                You are the computer-use subagent. Operate UI/browser/computer tools when enabled.
                Capture evidence screenshots and structured observations for verify stage.
                """,
                "Return JSON tool actions or done with evidence summary.")
        };

        return templates
            .GroupBy(t => BuildKey(t.Role, t.Version))
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }
}
