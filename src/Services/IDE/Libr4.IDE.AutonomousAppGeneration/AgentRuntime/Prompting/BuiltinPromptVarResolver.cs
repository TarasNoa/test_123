using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;

public sealed class BuiltinPromptVarResolver : IBuiltinPromptVarResolver
{
    private readonly ISkillManifestRegistry? _skills;
    private readonly IPlatformCapabilityBriefingService? _platformBriefing;

    public BuiltinPromptVarResolver(
        ISkillManifestRegistry? skills = null,
        IPlatformCapabilityBriefingService? platformBriefing = null)
    {
        _skills = skills;
        _platformBriefing = platformBriefing;
    }

    private static readonly string[] KnownVariables =
    [
        "LIBR4_STACK",
        "LIBR4_MANIFEST_FILES",
        "LIBR4_WORKSPACE_LS",
        "LIBR4_BUILD_LOG",
        "LIBR4_ERRORS",
        "LIBR4_RUN_ID",
        "LIBR4_STAGE",
        "LIBR4_REPAIR_ATTEMPT",
        "LIBR4_JIT_CONTEXT",
        "LIBR4_SKILLS_MANIFEST",
        "LIBR4_ACTIVE_SKILLS",
        "LIBR4_PLATFORM_CAPABILITIES"
    ];

    public string Resolve(string variableName, BuiltinPromptVarContext context)
    {
        var key = NormalizeKey(variableName);
        return key switch
        {
            "LIBR4_STACK" => ResolveStack(context.Plan),
            "LIBR4_MANIFEST_FILES" => FormatList(context.ManifestFiles, "(none)"),
            "LIBR4_WORKSPACE_LS" => WorkspaceSnapshotProvider.CaptureTree(
                context.WorkspaceHostPath ?? string.Empty,
                context.WorkspaceListDepth),
            "LIBR4_BUILD_LOG" => TailBuildLog(context.BuildLog, context.BuildLogTailLines),
            "LIBR4_ERRORS" => FormatList(context.LastErrors, "(none)"),
            "LIBR4_RUN_ID" => context.RunId?.ToString("D") ?? "(none)",
            "LIBR4_STAGE" => context.Stage.ToString().ToLowerInvariant(),
            "LIBR4_REPAIR_ATTEMPT" => context.RepairAttempt.ToString(),
            "LIBR4_JIT_CONTEXT" => string.IsNullOrWhiteSpace(context.JitLibr4Context)
                ? "(none — access a file path to load nearest LIBR4.md)"
                : context.JitLibr4Context!,
            "LIBR4_SKILLS_MANIFEST" => context.SkillsManifest
                                      ?? _skills?.FormatManifest()
                                      ?? "(skills manifest unavailable)",
            "LIBR4_ACTIVE_SKILLS" => context.ActivatedSkillNames.Count == 0
                ? "(none — use activate_skill to load SKILL.md)"
                : string.Join(", ", context.ActivatedSkillNames),
            "LIBR4_PLATFORM_CAPABILITIES" => context.PlatformCapabilities
                                      ?? ResolvePlatformCapabilities(context),
            _ => string.Empty
        };
    }

    public IReadOnlyDictionary<string, string> ResolveAll(BuiltinPromptVarContext context) =>
        KnownVariables.ToDictionary(v => v, v => Resolve(v, context), StringComparer.OrdinalIgnoreCase);

    private static string NormalizeKey(string variableName)
    {
        var trimmed = variableName.Trim();
        if (trimmed.StartsWith("{{", StringComparison.Ordinal) && trimmed.EndsWith("}}", StringComparison.Ordinal))
            trimmed = trimmed[2..^2].Trim();
        return trimmed.ToUpperInvariant();
    }

    private string ResolvePlatformCapabilities(BuiltinPromptVarContext context)
    {
        if (_platformBriefing is not null)
        {
            return _platformBriefing.BuildBriefing(new PlatformCapabilityBriefingRequest(
                MapStage(context.Stage),
                context.Plan,
                PlatformCapabilityBriefingScope.UserRequest));
        }

        return PlatformCapabilityBriefingScope.CurrentBriefing
               ?? "(scoped capabilities unavailable — use tool_search for JIT discovery)";
    }

    private static PlatformCapabilityBriefingStage MapStage(BuiltinPromptStage stage) =>
        stage switch
        {
            BuiltinPromptStage.Planning => PlatformCapabilityBriefingStage.Planning,
            BuiltinPromptStage.Generating => PlatformCapabilityBriefingStage.Generation,
            BuiltinPromptStage.Repairing => PlatformCapabilityBriefingStage.Repair,
            BuiltinPromptStage.Verify => PlatformCapabilityBriefingStage.Verify,
            _ => PlatformCapabilityBriefingStage.Repair
        };

    private static string ResolveStack(GenerationPlan? plan)
    {
        if (plan is null)
            return "(unknown)";

        var langs = string.Join(", ", plan.TechStack.Languages);
        var frameworks = string.Join(", ", plan.TechStack.Frameworks);
        return $"{langs} + {frameworks}";
    }

    private static string FormatList(IReadOnlyList<string> items, string empty)
    {
        if (items.Count == 0)
            return empty;
        return string.Join("\n", items.Select(i => $"- {i}"));
    }

    private static string TailBuildLog(string? buildLog, int tailLines)
    {
        if (string.IsNullOrWhiteSpace(buildLog))
            return "(empty)";

        var lines = buildLog.Replace("\r\n", "\n").Split('\n');
        if (lines.Length <= tailLines)
            return buildLog;

        var tail = lines[^tailLines..];
        return string.Join('\n', tail);
    }
}
