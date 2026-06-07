using Libr4.IDE.Application.AutonomousAppGeneration.Memory.Profile;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Memory.Honcho;

public interface IHonchoMemoryService
{
    Task<string> AugmentPlanningRequestAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        string? projectWorkspacePath = null,
        CancellationToken ct = default);

    Task SyncRunAsync(
        AppGenerationOrchestrator orchestrator,
        string? projectWorkspacePath = null,
        CancellationToken ct = default);

    Task<HonchoChatResult> ReasonAsync(
        string userId,
        string projectKey,
        string sessionId,
        string query,
        CancellationToken ct = default);

    Task<PersonaDocument?> GetPersonaAsync(
        string userId,
        string projectKey,
        CancellationToken ct = default);
}

public sealed class HonchoMemoryService : IHonchoMemoryService
{
    private readonly HonchoMemoryOptions _options;
    private readonly IHonchoMemoryClient _client;
    private readonly IPersonaStore _personaStore;
    private readonly IUserProfileService? _userProfiles;
    private readonly ILogger<HonchoMemoryService> _logger;

    public HonchoMemoryService(
        IOptions<HonchoMemoryOptions> options,
        IHonchoMemoryClient client,
        IPersonaStore personaStore,
        ILogger<HonchoMemoryService> logger,
        IUserProfileService? userProfiles = null)
    {
        _options = options.Value;
        _client = client;
        _personaStore = personaStore;
        _logger = logger;
        _userProfiles = userProfiles;
    }

    public async Task<string> AugmentPlanningRequestAsync(
        AppGenerationOrchestrator orchestrator,
        string userRequest,
        string? projectWorkspacePath = null,
        CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(userRequest))
            return userRequest;

        var userId = ResolveUserId(orchestrator);
        if (userId is null)
            return userRequest;

        var projectKey = HonchoProjectKeyResolver.Resolve(orchestrator, projectWorkspacePath);
        var persona = await _personaStore.LoadAsync(userId, projectKey, ct).ConfigureAwait(false);

        string? remoteSupplement = null;
        if (_client.IsRemoteEnabled)
        {
            var sessionId = HonchoProjectKeyResolver.ResolveSessionId(orchestrator, projectKey);
            var chat = await _client.ChatAsync(
                new HonchoChatRequest(
                    userId,
                    sessionId,
                    "Summarize the user's goals, preferences, and recurring patterns relevant to planning a new app generation run.",
                    _options.DefaultReasoningLevel),
                ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(chat.Content))
                remoteSupplement = chat.Content;
        }

        if (persona is null && string.IsNullOrWhiteSpace(remoteSupplement))
            return userRequest;

        var sections = new List<string>();
        if (persona is not null)
            sections.Add(persona.ToPlanningSection(_options.MaxPlanningChars / 2));

        if (!string.IsNullOrWhiteSpace(remoteSupplement))
        {
            sections.Add("## honcho_dialectic");
            sections.Add(remoteSupplement.Trim());
        }

        var honchoBlock = string.Join("\n\n", sections).Trim();
        if (honchoBlock.Length > _options.MaxPlanningChars)
            honchoBlock = honchoBlock[.._options.MaxPlanningChars] + "…";

        return $"{honchoBlock}\n\n{userRequest}";
    }

    public async Task SyncRunAsync(
        AppGenerationOrchestrator orchestrator,
        string? projectWorkspacePath = null,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return;

        var userId = ResolveUserId(orchestrator);
        if (userId is null)
            return;

        if (orchestrator.Status != GenerationStatus.Completed
            && orchestrator.Status != GenerationStatus.Failed)
        {
            return;
        }

        var projectKey = HonchoProjectKeyResolver.Resolve(orchestrator, projectWorkspacePath);
        var sessionId = HonchoProjectKeyResolver.ResolveSessionId(orchestrator, projectKey);
        var persona = await _personaStore.LoadAsync(userId, projectKey, ct).ConfigureAwait(false)
                      ?? new PersonaDocument { UserId = userId, ProjectKey = projectKey };

        var userMessage = BuildUserRunMessage(orchestrator);
        var assistantMessage = BuildAssistantRunMessage(orchestrator);

        if (_client.IsRemoteEnabled)
        {
            await _client.EnsurePeerAsync(userId, ct).ConfigureAwait(false);
            await _client.EnsurePeerAsync(_options.AgentPeerId, ct).ConfigureAwait(false);
            await _client.EnsureSessionAsync(sessionId, ct).ConfigureAwait(false);
            await _client.AppendMessagesAsync(
                sessionId,
                userId,
                _options.AgentPeerId,
                userMessage,
                assistantMessage,
                ct).ConfigureAwait(false);
        }

        var dialecticQuery =
            "What durable conclusions about this user's preferences, communication style, and project goals can we derive from this run?";
        var chat = _client.IsRemoteEnabled
            ? await _client.ChatAsync(new HonchoChatRequest(userId, sessionId, dialecticQuery, _options.DefaultReasoningLevel), ct)
                .ConfigureAwait(false)
            : new HonchoChatResult(BuildLocalDialectic(orchestrator), false);

        ApplyLocalPersonaUpdates(persona, orchestrator, chat.Content);
        persona.UpdatedAtUtc = DateTime.UtcNow;
        await _personaStore.SaveAsync(persona, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Synced Honcho persona for user {UserId} project {ProjectKey} run {RunId} remote={Remote}",
            userId,
            projectKey,
            orchestrator.Id,
            chat.FromRemote);
    }

    public async Task<HonchoChatResult> ReasonAsync(
        string userId,
        string projectKey,
        string sessionId,
        string query,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return new HonchoChatResult(string.Empty, false);

        if (_client.IsRemoteEnabled)
            return await _client.ChatAsync(new HonchoChatRequest(userId, sessionId, query, _options.DefaultReasoningLevel), ct)
                .ConfigureAwait(false);

        var persona = await _personaStore.LoadAsync(userId, projectKey, ct).ConfigureAwait(false);
        if (persona is null)
            return new HonchoChatResult("(no persona available)", false);

        return new HonchoChatResult(persona.ToPlanningSection(_options.MaxPlanningChars), false);
    }

    public Task<PersonaDocument?> GetPersonaAsync(
        string userId,
        string projectKey,
        CancellationToken ct = default) =>
        _personaStore.LoadAsync(userId, projectKey, ct);

    private void ApplyLocalPersonaUpdates(PersonaDocument persona, AppGenerationOrchestrator orchestrator, string? dialecticContent)
    {
        if (orchestrator.Plan is not null)
        {
            var stack = orchestrator.Plan.TechStack.Frameworks.FirstOrDefault()
                        ?? orchestrator.Plan.TechStack.Languages.FirstOrDefault()
                        ?? "unknown";
            var pattern = $"{orchestrator.Plan.ApplicationName} ({stack})";
            if (!persona.ProjectPatterns.Any(p => string.Equals(p, pattern, StringComparison.OrdinalIgnoreCase)))
                persona.ProjectPatterns.Add(pattern);
        }

        if (orchestrator.Status == GenerationStatus.Completed)
        {
            var goal = Truncate(orchestrator.UserRequest.Replace('\n', ' ').Trim(), 120);
            if (!persona.Goals.Any(g => string.Equals(g, goal, StringComparison.OrdinalIgnoreCase)))
                persona.Goals.Add(goal);
        }

        if (!string.IsNullOrWhiteSpace(dialecticContent))
        {
            foreach (var line in dialecticContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (line.Length < 8)
                    continue;

                persona.Conclusions.RemoveAll(c => string.Equals(c.Text, line, StringComparison.OrdinalIgnoreCase));
                persona.Conclusions.Add(new PersonaConclusionEntry(
                    line.TrimStart('-', ' ', '*'),
                    _client.IsRemoteEnabled ? "honcho" : "local",
                    DateTime.UtcNow));
            }
        }

        while (persona.Conclusions.Count > _options.MaxPersonaConclusions)
            persona.Conclusions.RemoveAt(0);

        if (string.IsNullOrWhiteSpace(persona.CommunicationStyle))
            persona.CommunicationStyle = orchestrator.Status == GenerationStatus.Completed
                ? "prefers iterative autonomous generation with verify gate"
                : "direct failure-oriented feedback";
    }

    private static string BuildLocalDialectic(AppGenerationOrchestrator orchestrator)
    {
        var lines = new List<string>();
        if (orchestrator.Status == GenerationStatus.Completed)
            lines.Add($"- User successfully generated `{orchestrator.Plan?.ApplicationName}` with verify-friendly patterns.");
        else
            lines.Add($"- User hit failures in `{orchestrator.Plan?.ApplicationName ?? "unknown app"}`: {orchestrator.FailureReason ?? "unknown"}.");

        foreach (var failure in orchestrator.Iterations.SelectMany(i => i.Errors).Take(3))
            lines.Add($"- Recurring error theme: {failure.ErrorType} in {failure.FilePath}");

        return string.Join('\n', lines);
    }

    private static string BuildUserRunMessage(AppGenerationOrchestrator orchestrator) =>
        $"User request: {orchestrator.UserRequest}\nStatus: {orchestrator.Status}\nFingerprint: {orchestrator.RequestFingerprint}";

    private static string BuildAssistantRunMessage(AppGenerationOrchestrator orchestrator)
    {
        var app = orchestrator.Plan?.ApplicationName ?? "unknown";
        var files = orchestrator.Files.Count;
        var iterations = orchestrator.Iterations.Count;
        return $"Libr4 run {orchestrator.Id:D} for `{app}` finished with status={orchestrator.Status}, files={files}, iterations={iterations}, failure={orchestrator.FailureReason ?? "none"}.";
    }

    private string? ResolveUserId(AppGenerationOrchestrator orchestrator) =>
        _userProfiles?.ResolveUserId(orchestrator) ?? UserProfileIdentityResolver.Resolve(orchestrator);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}

public sealed class HonchoMemoryFinalizationHook : IAutonomousFinalizationHook
{
    private readonly IHonchoMemoryService _honcho;

    public HonchoMemoryFinalizationHook(IHonchoMemoryService honcho) => _honcho = honcho;

    public int Order => 86;

    public string Name => "honcho_persona_sync";

    public Task ExecuteAsync(AppGenerationOrchestrator orchestrator, CancellationToken ct) =>
        _honcho.SyncRunAsync(orchestrator, ct: ct);
}
