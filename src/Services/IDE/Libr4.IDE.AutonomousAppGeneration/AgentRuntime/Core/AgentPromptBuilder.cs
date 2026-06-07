using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public static class AgentPromptBuilder
{
    public const string RuntimeContextBlock = """

        RUNTIME CONTEXT (auto-injected):
        - Run: {{LIBR4_RUN_ID}} | Stage: {{LIBR4_STAGE}} | Repair attempt: {{LIBR4_REPAIR_ATTEMPT}}
        - Stack: {{LIBR4_STACK}}
        - Manifest files:
        {{LIBR4_MANIFEST_FILES}}
        - Workspace tree:
        {{LIBR4_WORKSPACE_LS}}
        - Recent build log:
        {{LIBR4_BUILD_LOG}}
        - Structured errors:
        {{LIBR4_ERRORS}}
        - Directory context (jit):
        {{LIBR4_JIT_CONTEXT}}
        - Skills manifest (activate_skill to load full SKILL.md):
        {{LIBR4_SKILLS_MANIFEST}}
        - Active skills this session:
        {{LIBR4_ACTIVE_SKILLS}}
        - Platform capabilities (scoped for stack/stage — use tool_search for more):
        {{LIBR4_PLATFORM_CAPABILITIES}}
        """;

    public const string SystemPrompt = """
        You are an autonomous coding agent operating inside an isolated shadow workspace (Claude Code paradigm).

        You MUST respond with ONLY a single JSON object per turn — no prose, no markdown fences.

        To invoke a tool:
        {"action":"tool","tool":"<tool_name>","input":{...}}

        When the task is complete (build fixed, files updated, objective met):
        {"action":"done","summary":"what you fixed"}

        EXTENDED TOOLS (Claude Code parity):
        - inspect_environment: host toolchain snapshot (/doctor)
        - run_build: run plan build commands (compile/install)
        - run_tests: run plan test commands only (separates test vs compile failures)
        - tool_search: discover tools/skills/MCP
        - todo_write: track repair subtasks in session
        - enter_plan_mode / exit_plan_mode: investigate read-only before edits
        - checkpoint: save/restore workspace file snapshots
        - activate_skill / mcp / agent: optional delegation (when enabled)

        RULES:
        1. Investigate before editing: inspect_environment, read_file, grep, run_build.
        2. Prefer edit_file (search/replace) over write_file for existing files.
        3. bash runs in the workspace root; use cd backend/ when needed.
        4. On Windows/WSL hosts use "python -m pip" not bare "pip" when pip is missing.
        5. Fix ROOT CAUSE from build log — do not guess.
        6. One tool per turn. Wait for tool result before next action.
        7. Paths are POSIX relative: backend/..., frontend/...
        8. Use enter_plan_mode when unsure; exit_plan_mode before edits.
        """ + RuntimeContextBlock;

    public const string GenerationSystemPrompt = """
        You are an autonomous code generation agent (Claude Code paradigm).
        Generate production-ready source files one at a time using tools.

        You MUST respond with ONLY a single JSON object per turn — no prose, no markdown fences.

        To invoke a tool:
        {"action":"tool","tool":"<tool_name>","input":{...}}

        When the assigned file(s) are fully written and complete:
        {"action":"done","summary":"what you created"}

        EXTENDED TOOLS:
        - inspect_environment, tool_search, todo_write, glob/grep for discovery
        - checkpoint: rollback bad writes during generation
        - enter_plan_mode / exit_plan_mode for read-only exploration

        RULES:
        1. NEW target files: call write_file directly — read_file is NOT required first.
        2. At most 2 investigation tools (glob/grep/read_file) then you MUST write_file.
        3. Use write_file for new files; edit_file only to adjust existing ones (read_file first for edit).
        4. Emit COMPLETE file content — no placeholders, no TODO stubs, no "..." omissions.
        5. Match the planned stack exactly (imports, frameworks, layout).
        6. One tool per turn. Never claim a file is created in summary — only write_file creates files.
        7. Respond with done ONLY after write_file succeeded for every TARGET FILE.
        8. FEATURE BATCH: when multiple TARGET FILES are listed, write ALL of them (one write_file per file) before done.
        """ + RuntimeContextBlock;

    private const string PlanningStageBlock = """
        STAGE=planning: read-only exploration. Use list_directory, glob, grep, read_file. Do not edit files.
        """;

    private const string GeneratingStageBlock = """
        STAGE=generating: write complete target files. Minimize investigation; prioritize write_file.
        """;

    private const string RepairingStageBlock = """
        STAGE=repairing: fix build/test failures from BUILD LOG and ERRORS. Prefer surgical edit_file/apply_patch.
        """;

    private const string VerifyStageBlock = """
        STAGE=verify: read-only validation. Run tests/build, inspect outputs, report pass/fail — no file edits.
        """;

    public const string DefaultResponseHint =
        "Return ONLY a single JSON object per turn — no prose, no markdown fences.";

    public static string BuildSystemPrompt(
        bool isGeneration,
        BuiltinPromptStage stage,
        IBuiltinPromptVarResolver resolver,
        BuiltinPromptVarContext context,
        IPromptTemplateRegistry? templates = null,
        string? promptVariant = null,
        string? explicitRole = null)
    {
        var basePrompt = isGeneration ? GenerationSystemPrompt : SystemPrompt;
        var withVars = PromptVariableSubstitutor.Apply(basePrompt, resolver, context);
        var stageBlock = stage switch
        {
            BuiltinPromptStage.Planning => PlanningStageBlock,
            BuiltinPromptStage.Generating => GeneratingStageBlock,
            BuiltinPromptStage.Verify => VerifyStageBlock,
            _ => RepairingStageBlock
        };

        var body = withVars + Environment.NewLine + stageBlock;
        if (templates is not null)
        {
            var role = explicitRole ?? MapStageToRole(stage, isGeneration);
            var rolePrompt = templates.FormatRolePrompt(role, promptVariant);
            if (!string.IsNullOrWhiteSpace(rolePrompt))
                body = rolePrompt + "\n\n" + body;
            return InstructionTemplateFormatter.Format(body, DefaultResponseHint);
        }

        return body;
    }

    public static string MapStageToRole(BuiltinPromptStage stage, bool isGeneration) =>
        stage switch
        {
            BuiltinPromptStage.Planning => "explore",
            BuiltinPromptStage.Verify => "verify",
            BuiltinPromptStage.Generating => "implementer",
            _ => isGeneration ? "implementer" : "repair"
        };

    public static string BuildUserObjective(
        string objective,
        GenerationPlan plan,
        string? buildLog,
        IAgentToolRegistry registry,
        IBuiltinPromptVarResolver? resolver = null,
        BuiltinPromptVarContext? context = null,
        string? contextFragments = null,
        bool useInstructionTemplate = false)
    {
        var langs = string.Join(", ", plan.TechStack.Languages);
        var frameworks = string.Join(", ", plan.TechStack.Frameworks);
        var log = string.IsNullOrWhiteSpace(buildLog) ? "(empty)" : Truncate(buildLog, 16_000);
        var contextBlock = !string.IsNullOrWhiteSpace(contextFragments)
            ? $"""
              CONTEXT FRAGMENTS (bounded repair context — investigate first):
              {contextFragments}
              """
            : $"""
              BUILD LOG (investigate this first):
              {log}
              """;

        var body = $"""
            OBJECTIVE:
            {objective}

            APPLICATION: {plan.ApplicationName}
            STACK: {langs} + {frameworks}
            BUILD COMMANDS: {string.Join(" ; ", plan.BuildCommands)}
            TEST COMMANDS: {string.Join(" ; ", plan.TestCommands)}

            {contextBlock}

            AVAILABLE TOOLS:
            {BuildToolCatalog(registry)}
            """;

        var resolved = resolver is not null && context is not null
            ? PromptVariableSubstitutor.Apply(body, resolver, context)
            : body;

        return useInstructionTemplate
            ? InstructionTemplateFormatter.Format(resolved, DefaultResponseHint)
            : resolved;
    }

    public static string BuildGenerationObjective(
        string fileObjective,
        GenerationPlan plan,
        IReadOnlyList<string> targetPaths,
        IAgentToolRegistry registry,
        IBuiltinPromptVarResolver? resolver = null,
        BuiltinPromptVarContext? context = null,
        bool useInstructionTemplate = false)
    {
        var langs = string.Join(", ", plan.TechStack.Languages);
        var frameworks = string.Join(", ", plan.TechStack.Frameworks);
        var targets = targetPaths.Count == 0
            ? "(none)"
            : string.Join("\n", targetPaths.Select(p => $"- {p}"));

        var body = $"""
            GENERATION TASK:
            {fileObjective}

            TARGET FILE(S) — write ONLY these paths:
            {targets}

            APPLICATION: {plan.ApplicationName}
            DESCRIPTION: {plan.ApplicationDescription}
            STACK: {langs} + {frameworks}

            WORKSPACE: may be empty for new files — write_file does NOT require prior read_file.
            Use at most 2 glob/grep/read_file calls, then write_file with full content.
            Do NOT emit batch JSON. Do NOT return done without a successful write_file.

            AVAILABLE TOOLS:
            {BuildToolCatalog(registry)}
            """;

        var resolved = resolver is not null && context is not null
            ? PromptVariableSubstitutor.Apply(body, resolver, context)
            : body;

        return useInstructionTemplate
            ? InstructionTemplateFormatter.Format(resolved, DefaultResponseHint)
            : resolved;
    }

    public static string BuildToolCatalog(IAgentToolRegistry registry)
    {
        if (registry is FilteredAgentToolRegistry filtered)
            return filtered.BuildToolCatalog();

        return registry.BuildToolCatalog();
    }

    public static string BuildTurnPrompt(IReadOnlyList<AgentConversationTurn> turns)
    {
        if (turns.Count == 0)
            return string.Empty;

        var sb = new System.Text.StringBuilder();
        foreach (var turn in turns)
        {
            sb.AppendLine($"=== {turn.Role.ToUpperInvariant()} ===");
            sb.AppendLine(turn.Content);
            sb.AppendLine();
        }

        sb.AppendLine("Respond with the next JSON action (tool or done).");
        return sb.ToString();
    }

    private static string Truncate(string text, int max)
    {
        if (text.Length <= max)
            return text;
        return text[..max] + "\n...[truncated]...";
    }
}
