using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public interface ISpaceContextFanout
{
    Task FanoutAsync(SpaceContextEvent evt, CancellationToken ct = default);
}

public sealed class SpaceContextNdjsonFanout : ISpaceContextFanout
{
    private readonly ISpaceStore _store;
    private readonly INdjsonEventWriter _ndjson;
    private readonly IDMailBus? _dmail;
    private readonly ILogger<SpaceContextNdjsonFanout> _logger;

    public SpaceContextNdjsonFanout(
        ISpaceStore store,
        INdjsonEventWriter ndjson,
        ILogger<SpaceContextNdjsonFanout> logger,
        IDMailBus? dmail = null)
    {
        _store = store;
        _ndjson = ndjson;
        _dmail = dmail;
        _logger = logger;
    }

    public async Task FanoutAsync(SpaceContextEvent evt, CancellationToken ct = default)
    {
        var envelope = new
        {
            type = "space_context_updated",
            spaceId = evt.SpaceId,
            eventId = evt.EventId,
            kind = evt.Kind,
            title = evt.Title,
            payload = evt.Payload,
            authorMemberId = evt.AuthorMemberId,
            timestampUtc = evt.TimestampUtc
        };

        var members = await _store.ListMembersAsync(evt.SpaceId, ct).ConfigureAwait(false);
        foreach (var member in members)
        {
            if (member.RunId is not Guid runId)
                continue;

            await _ndjson.WriteAsync(runId, envelope, ct).ConfigureAwait(false);
        }

        if (_dmail is not null && IsHandoffKind(evt.Kind))
        {
            foreach (var role in new[] { SpaceMemberRole.Implementer, SpaceMemberRole.Verifier })
            {
                var target = members.FirstOrDefault(m => m.Role == role && m.RunId is not null);
                if (target?.RunId is null)
                    continue;

                var address = BuildDMailAddress(evt.SpaceId, role);
                await _dmail.SendAsync(
                    target.RunId.Value,
                    evt.AuthorMemberId ?? "space-bus",
                    address,
                    $"[{evt.Kind}] {evt.Title}\n{evt.Payload}",
                    ackRequired: false,
                    ct).ConfigureAwait(false);
            }
        }

        _logger.LogDebug("Fanout space context {Kind} to {Count} runs", evt.Kind, members.Count(m => m.RunId is not null));
    }

    private static bool IsHandoffKind(string kind) =>
        kind is "space_context_ready" or "explorer_complete" or "plan" or "plan_summary" or "implementer_checkpoint";

    private static string BuildDMailAddress(Guid spaceId, SpaceMemberRole role) =>
        $"@space/{spaceId:D}/{role.ToString().ToLowerInvariant()}";
}
