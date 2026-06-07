namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

internal static class PlatformScopedCapabilityCatalog
{
    internal sealed record CapabilityCard(
        string Name,
        string Tools,
        string WhenUse,
        string WhenNotUse,
        PlatformCapabilityBriefingStage[] Stages,
        Func<PlatformStackProfile, bool>? StackFilter = null);

    internal static IReadOnlyList<CapabilityCard> All { get; } =
    [
        new(
            "File edit & read",
            "read_file, edit_file, apply_patch, write_file, glob, grep",
            "Inspect errors, patch existing files, locate symbols.",
            "Do not re-read the same file repeatedly; do not write_file when edit_file suffices.",
            [PlatformCapabilityBriefingStage.Generation, PlatformCapabilityBriefingStage.Repair, PlatformCapabilityBriefingStage.ErrorAnalysis]),
        new(
            "Shell & build",
            "bash, run_build, run_tests",
            "Install deps, run plan build/test commands, reproduce failures.",
            "Do not bash-explore when a targeted patch fixes the error; avoid rm -rf or destructive commands.",
            [PlatformCapabilityBriefingStage.Generation, PlatformCapabilityBriefingStage.Repair, PlatformCapabilityBriefingStage.Verify]),
        new(
            "Stack skill",
            "activate_skill",
            "Unfamiliar stack patterns, project layout, idioms for this framework.",
            "Do not activate for trivial typos or one-line import fixes.",
            [PlatformCapabilityBriefingStage.Generation, PlatformCapabilityBriefingStage.Repair],
            stack => stack.SkillIds.Count > 0),
        new(
            "Pytest / import repair",
            "read_file, apply_patch, bash (pip), run_tests",
            "ModuleNotFoundError, wrong src/ layout, conftest path, missing __init__.py.",
            "Do not delegate or browser-research for standard pytest path issues.",
            [PlatformCapabilityBriefingStage.Repair, PlatformCapabilityBriefingStage.ErrorAnalysis],
            stack => stack.IsPython),
        new(
            "Browser research",
            "browser_research, browser_*",
            "Unknown external API, docs lookup, UI verify/smoke evidence.",
            "Do not browse for errors already visible in build log; skip for pure backend CRUD.",
            [PlatformCapabilityBriefingStage.Planning, PlatformCapabilityBriefingStage.CascadePlanning, PlatformCapabilityBriefingStage.Verify]),
        new(
            "Codebase search",
            "grep, glob, search_codebase",
            "Find patterns across many files, upstream clone context.",
            "Do not search when the failing file path is already known.",
            [PlatformCapabilityBriefingStage.Planning, PlatformCapabilityBriefingStage.Repair, PlatformCapabilityBriefingStage.CascadePlanning]),
        new(
            "Hermes memory",
            "memory_read, memory_write",
            "Similar project ran before; reuse proven fix patterns.",
            "Do not memory_search on every turn; orchestrator already prefetches when relevant.",
            [PlatformCapabilityBriefingStage.Planning, PlatformCapabilityBriefingStage.Repair]),
        new(
            "Subagent (explore / verify)",
            "subagent",
            "Large unfamiliar codebase, dedicated verify recipe, parallel read-only exploration.",
            "Do not spawn subagents for single-file compile errors or small CRUD apps.",
            [PlatformCapabilityBriefingStage.Repair, PlatformCapabilityBriefingStage.Verify]),
        new(
            "Delegation",
            "delegate",
            "Independent subtask touching 30+ files or long background work.",
            "Do not delegate the main repair loop or small fixes.",
            [PlatformCapabilityBriefingStage.Repair]),
        new(
            "Plan mode",
            "enter_plan_mode, todo_write, exit_plan_mode",
            "Multi-file refactor plan before edits.",
            "Do not enter plan mode for one obvious patch.",
            [PlatformCapabilityBriefingStage.Repair, PlatformCapabilityBriefingStage.Generation]),
        new(
            "MCP external tools",
            "mcp, mcp_agent_tool",
            "Third-party API/tooling only available via MCP.",
            "Do not invoke MCP for standard file/bash/test fixes.",
            [PlatformCapabilityBriefingStage.Planning, PlatformCapabilityBriefingStage.Repair]),
        new(
            "JIT discovery",
            "tool_search",
            "Need a capability not listed here (skills, MCP, subagents).",
            "Do not call tool_search every turn — only when stuck or explicitly need more tools.",
            Enum.GetValues<PlatformCapabilityBriefingStage>()),
    ];

    internal static IReadOnlyList<CapabilityCard> Select(
        PlatformCapabilityBriefingStage stage,
        PlatformStackProfile stack)
    {
        return All
            .Where(c => c.Stages.Contains(stage))
            .Where(c => c.StackFilter is null || c.StackFilter(stack))
            .ToList();
    }

    internal static int DeferredCount(PlatformCapabilityBriefingStage stage, PlatformStackProfile stack) =>
        Math.Max(0, All.Count - Select(stage, stack).Count);
}
