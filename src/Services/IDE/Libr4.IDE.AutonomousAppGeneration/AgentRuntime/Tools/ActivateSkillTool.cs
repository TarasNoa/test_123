using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;

/// <summary>Progressive disclosure: load SKILL.md on demand (Gemini activate_skill paradigm).</summary>
public sealed class ActivateSkillTool : IAgentTool
{
    private readonly ISkillManifestRegistry _manifest;
    private readonly ISkillConsentGate _consent;
    private readonly IRolloutRecorder? _rollout;
    private readonly SkillActivationOptions _options;

    public ActivateSkillTool(
        ISkillManifestRegistry manifest,
        ISkillConsentGate consent,
        IOptions<SkillActivationOptions> options,
        IRolloutRecorder? rollout = null)
    {
        _manifest = manifest;
        _consent = consent;
        _rollout = rollout;
        _options = options.Value;
    }

    public string Name => "activate_skill";
    public string Description => "Load full SKILL.md instructions on demand. Input: { \"name\": \"python-django\" }";
    public bool IsReadOnly => true;
    public bool IsConcurrencySafe(JsonElement input) => true;

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken ct)
    {
        var skillName = ResolveSkillName(input);
        if (string.IsNullOrWhiteSpace(skillName))
            return Fail("name required");

        var entry = _manifest.Find(skillName);
        if (entry is null)
            return Fail($"skill not found: {skillName}. Use tool_search or check LIBR4_SKILLS_MANIFEST.");

        if (context.Session.ActivatedSkills.Contains(entry.Id))
        {
            return Ok($"""
                skill={entry.Id} status=already_active
                ---
                Skill already activated this session. Apply existing guidance; do not re-inject.
                """);
        }

        if (context.Session.ActivatedSkills.Count >= _options.MaxActivatedSkillsPerSession)
            return Fail($"session skill limit reached ({_options.MaxActivatedSkillsPerSession})");

        var consent = _consent.Evaluate(
            context.Session.RunId,
            entry.Id,
            _options.AutoApproveFirstActivation);
        if (consent.Status == SkillConsentStatus.Pending)
            return Fail(consent.Reason ?? "skill consent pending");
        if (consent.Status == SkillConsentStatus.Denied)
            return Fail(consent.Reason ?? "skill consent denied");

        if (context.Session.RunId is Guid runId)
            _consent.RecordGrant(runId, entry.Id);

        var content = await _manifest.LoadContentAsync(entry.Id, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
            return Fail($"SKILL.md empty or missing for: {entry.Id}");

        context.Session.ActivatedSkills.Add(entry.Id);

        var firstActivation = true;
        if (context.Session.RunId is Guid auditRunId && _rollout is not null)
        {
            await _rollout.RecordSkillActivationAsync(
                auditRunId,
                context.Session.SessionId,
                entry.Id,
                firstActivation,
                consentGranted: true,
                content.Length,
                ct).ConfigureAwait(false);
        }

        return Ok($"""
            skill={entry.Id}
            description={entry.Description}
            status=activated
            ---
            {content}
            ---
            Apply this skill guidance on subsequent turns. Do not dump skill text to user files.
            """);
    }

    private static string? ResolveSkillName(JsonElement input)
    {
        if (input.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            return name.GetString();
        if (input.TryGetProperty("skill_id", out var id) && id.ValueKind == JsonValueKind.String)
            return id.GetString();
        if (input.TryGetProperty("skill", out var skill) && skill.ValueKind == JsonValueKind.String)
            return skill.GetString();
        return null;
    }

    private static ToolExecutionResult Ok(string output) =>
        new("activate_skill", true, output, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());

    private static ToolExecutionResult Fail(string msg) =>
        new("activate_skill", false, msg, Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
}
