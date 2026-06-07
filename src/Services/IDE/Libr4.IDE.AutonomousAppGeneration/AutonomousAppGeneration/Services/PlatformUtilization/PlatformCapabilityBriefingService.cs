using System.Text;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public sealed record PlatformCapabilityBriefingRequest(
    PlatformCapabilityBriefingStage Stage = PlatformCapabilityBriefingStage.Generation,
    GenerationPlan? Plan = null,
    string? UserRequest = null);

public interface IPlatformCapabilityBriefingService
{
    string BuildBriefing(PlatformCapabilityBriefingRequest? request = null);
}

public sealed class PlatformCapabilityBriefingService : IPlatformCapabilityBriefingService
{
    private readonly IAgentToolRegistry _tools;
    private readonly ISkillManifestRegistry _skills;
    private readonly IAgentSpecRegistry _agentSpecs;
    private readonly IFlowRegistry? _flowRegistry;
    private readonly AutonomousPlatformUtilizationOptions _options;

    public PlatformCapabilityBriefingService(
        IAgentToolRegistry tools,
        ISkillManifestRegistry skills,
        IAgentSpecRegistry agentSpecs,
        IOptions<AutonomousPlatformUtilizationOptions> options,
        IFlowRegistry? flowRegistry = null)
    {
        _tools = tools;
        _skills = skills;
        _agentSpecs = agentSpecs;
        _flowRegistry = flowRegistry;
        _options = options.Value;
    }

    public string BuildBriefing(PlatformCapabilityBriefingRequest? request = null)
    {
        request ??= new PlatformCapabilityBriefingRequest();
        var text = _options.CapabilityBriefingMode == PlatformCapabilityBriefingMode.Full
            ? BuildFullBriefing(request.Plan)
            : BuildScopedBriefing(request);

        if (text.Length <= _options.MaxBriefingChars)
            return text;

        return text[.._options.MaxBriefingChars] + "\n...(briefing truncated)";
    }

    private string BuildScopedBriefing(PlatformCapabilityBriefingRequest request)
    {
        var stack = ResolveStackProfile(request.Plan, request.UserRequest);
        var cards = PlatformScopedCapabilityCatalog.Select(request.Stage, stack);
        var deferred = PlatformScopedCapabilityCatalog.DeferredCount(request.Stage, stack);

        var sb = new StringBuilder();
        sb.AppendLine("Libr4 scoped capabilities — use only when criteria match. Ship working build+tests.");
        sb.AppendLine($"Stage: {request.Stage.ToString().ToLowerInvariant()} | Stack: {stack.Summary}");
        sb.AppendLine("Do not call tools for exploration when a direct file/bash/test fix is obvious.");
        sb.AppendLine();

        if (stack.SkillIds.Count > 0)
        {
            sb.AppendLine("Relevant stack skills (activate_skill when needed):");
            foreach (var skillId in stack.SkillIds)
            {
                var skill = _skills.Find(skillId);
                var desc = skill?.Description ?? skillId;
                sb.Append("- ").Append(skillId).Append(": ").AppendLine(TruncateOneLine(desc, 90));
            }

            sb.AppendLine();
        }

        sb.AppendLine("### Injected for this run");
        foreach (var card in cards)
        {
            sb.Append("- **").Append(card.Name).Append("** (").Append(card.Tools).AppendLine(")");
            sb.Append("  USE: ").AppendLine(card.WhenUse);
            sb.Append("  SKIP: ").AppendLine(card.WhenNotUse);
        }

        if (deferred > 0)
        {
            sb.AppendLine();
            sb.Append("Other platform capabilities (").Append(deferred)
                .AppendLine(") exist but are filtered out for this stack/stage.");
            sb.AppendLine("Call tool_search(\"keyword\") only when you need one of them.");
        }

        AppendPlanCommands(sb, request.Plan);
        return sb.ToString().Trim();
    }

    private string BuildFullBriefing(GenerationPlan? plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You operate inside the Libr4 autonomous app-generation platform.");
        sb.AppendLine("Full catalog mode — prefer scoped tools; use tool_search to narrow.");
        sb.AppendLine();
        sb.AppendLine("## Agent tools");
        sb.AppendLine(_tools.BuildToolCatalog());
        sb.AppendLine("## Subagents");
        foreach (var spec in _agentSpecs.All.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase))
            sb.Append("- ").Append(spec.Name).AppendLine();
        sb.AppendLine("## Skills");
        sb.AppendLine(_skills.FormatManifest());
        AppendPlanCommands(sb, plan);
        return sb.ToString().Trim();
    }

    private static PlatformStackProfile ResolveStackProfile(GenerationPlan? plan, string? userRequest)
    {
        if (plan is not null)
        {
            var fromPlan = string.Join(' ',
                plan.TechStack.Languages.Concat(plan.TechStack.Frameworks));
            if (!string.IsNullOrWhiteSpace(fromPlan))
                return PlatformStackProfile.FromBlob(fromPlan);
        }

        return PlatformStackProfile.FromBlob(userRequest ?? string.Empty);
    }

    private static void AppendPlanCommands(StringBuilder sb, GenerationPlan? plan)
    {
        if (plan is null)
            return;

        sb.AppendLine();
        sb.AppendLine("### Plan commands");
        foreach (var cmd in plan.BuildCommands)
            sb.Append("- build: ").AppendLine(cmd);
        foreach (var cmd in plan.TestCommands)
            sb.Append("- test: ").AppendLine(cmd);
    }

    private static string TruncateOneLine(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        var one = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return one.Length <= max ? one : one[..max] + "...";
    }
}
