using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentIntegration.McpHost;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Search;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

public interface IPlatformRunBootstrapService
{
    Task<PlatformRunBootstrapResult> BeginRunAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        CancellationToken ct = default);

    Task AfterPlanAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        CancellationToken ct = default);
}

public sealed record PlatformRunBootstrapResult(
    string Briefing,
    IReadOnlyList<string> ActivatedFeatures,
    IDisposable BriefingScope);

public sealed class PlatformRunBootstrapService : IPlatformRunBootstrapService
{
    private readonly IPlatformCapabilityBriefingService _briefing;
    private readonly AutonomousPlatformUtilizationOptions _options;
    private readonly ISkillManifestRegistry _skills;
    private readonly ISkillConsentGate _skillConsent;
    private readonly IMcpRunHostManager? _mcpRunHost;
    private readonly ISessionSearchService? _sessionSearch;
    private readonly ILogger<PlatformRunBootstrapService> _logger;

    public PlatformRunBootstrapService(
        IPlatformCapabilityBriefingService briefing,
        IOptions<AutonomousPlatformUtilizationOptions> options,
        ISkillManifestRegistry skills,
        ISkillConsentGate skillConsent,
        ILogger<PlatformRunBootstrapService> logger,
        IMcpRunHostManager? mcpRunHost = null,
        ISessionSearchService? sessionSearch = null)
    {
        _briefing = briefing;
        _options = options.Value;
        _skills = skills;
        _skillConsent = skillConsent;
        _mcpRunHost = mcpRunHost;
        _sessionSearch = sessionSearch;
        _logger = logger;
    }

    public async Task<PlatformRunBootstrapResult> BeginRunAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_options.EnableFullPlatformUtilization)
        {
            return new PlatformRunBootstrapResult(
                string.Empty,
                Array.Empty<string>(),
                PlatformCapabilityBriefingScope.Begin(string.Empty));
        }

        var activated = new List<string> { "full_platform_utilization" };
        var briefingText = string.Empty;

        if (_options.InjectCapabilityBriefing)
        {
            activated.Add(_options.CapabilityBriefingMode == PlatformCapabilityBriefingMode.Scoped
                ? "capability_briefing_scoped"
                : "capability_briefing_full");
        }

        if (_options.WarmMcpRunHost && _mcpRunHost is not null && _mcpRunHost.IsUnifiedHostEnabled)
        {
            activated.Add("mcp_unified_host");
            _ = _mcpRunHost.DiscoverServers();
        }

        if (_options.PrefetchRunMemory && _sessionSearch is not null)
        {
            try
            {
                var hits = await _sessionSearch.SearchAsync(userRequest, limit: 5, ct).ConfigureAwait(false);
                if (hits.Count > 0)
                {
                    activated.Add($"memory_prefetch:{hits.Count}");
                    orchestrator.RecordMemoryRetrieval(new MemoryRetrievalAuditEntry(
                        orchestrator.Id,
                        "planning",
                        MemoryKind.Semantic,
                        "platform_bootstrap",
                        $"prefetch_hits={hits.Count}",
                        "full_platform_bootstrap",
                        hits[0].Score,
                        DateTime.UtcNow));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[AutoGen {Id}] Memory prefetch skipped", orchestrator.Id);
            }
        }

        orchestrator.RecordQualityGate(
            "platform_utilization",
            10,
            true,
            activated.ToArray());

        var scope = _options.InjectCapabilityBriefing
            ? PlatformCapabilityBriefingScope.BeginWithService(
                _briefing,
                activated,
                userRequest)
            : PlatformCapabilityBriefingScope.Begin(string.Empty, activated, userRequest);

        _logger.LogInformation(
            "[AutoGen {Id}] Full platform bootstrap: mode={Mode}, features=[{Features}]",
            orchestrator.Id,
            _options.CapabilityBriefingMode,
            string.Join(", ", activated));

        return new PlatformRunBootstrapResult(string.Empty, activated, scope);
    }

    public Task AfterPlanAsync(
        AppGenerationOrchestrator orchestrator,
        GenerationPlan plan,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_options.EnableFullPlatformUtilization)
            return Task.CompletedTask;

        var activated = new List<string>();
        if (_options.AutoActivateStackSkills)
        {
            foreach (var skillId in ResolveStackSkillIds(plan))
            {
                if (_skills.Find(skillId) is null)
                    continue;

                _skillConsent.RecordGrant(orchestrator.Id, skillId);
                orchestrator.RecordSkillInvocation(new SkillInvocationAuditEntry(
                    skillId,
                    "1",
                    "platform_bootstrap",
                    "auto",
                    DateTime.UtcNow,
                    0,
                    "auto_granted",
                    "stack",
                    "full_platform_utilization",
                    null,
                    null,
                    null));
                activated.Add($"skill:{skillId}");
            }
        }

        if (activated.Count > 0)
        {
            orchestrator.RecordQualityGate(
                "platform_stack_skills",
                10,
                true,
                activated.ToArray());
            _logger.LogInformation(
                "[AutoGen {Id}] Auto-activated stack skills: {Skills}",
                orchestrator.Id,
                string.Join(", ", activated));
        }

        // Plan attached — scoped briefings will use stack + commands from here on.
        if (_options.InjectCapabilityBriefing)
            PlatformCapabilityBriefingScope.UpdatePlan(plan);

        return Task.CompletedTask;
    }

    private static IEnumerable<string> ResolveStackSkillIds(GenerationPlan plan)
    {
        var blob = string.Join(' ',
            plan.TechStack.Languages.Concat(plan.TechStack.Frameworks)).ToLowerInvariant();

        if (blob.Contains("fastapi", StringComparison.Ordinal))
            yield return "python-fastapi";
        if (blob.Contains("django", StringComparison.Ordinal))
            yield return "python-django";
        if (blob.Contains("next", StringComparison.Ordinal))
            yield return "ts-react";
        if (blob.Contains("spring", StringComparison.Ordinal) || blob.Contains("java", StringComparison.Ordinal))
            yield return "java-spring";
        if (blob.Contains("express", StringComparison.Ordinal))
            yield return "js-express";
        if (blob.Contains("nestjs", StringComparison.Ordinal) || blob.Contains("nest", StringComparison.Ordinal))
            yield return "ts-nestjs";
        if (blob.Contains("react", StringComparison.Ordinal) && !blob.Contains("next", StringComparison.Ordinal))
            yield return "js-react";
        if (blob.Contains("dotnet", StringComparison.Ordinal) || blob.Contains("asp.net", StringComparison.Ordinal))
            yield return "csharp-aspnet";
    }
}
