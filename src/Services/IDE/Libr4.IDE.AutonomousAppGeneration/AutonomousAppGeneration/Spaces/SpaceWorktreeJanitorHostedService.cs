using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class SpaceWorktreeJanitorHostedService : BackgroundService
{
    private readonly ISpaceStore _store;
    private readonly IGitWorktreeService _git;
    private readonly AgentSpaceOptions _options;
    private readonly ILogger<SpaceWorktreeJanitorHostedService> _logger;

    public SpaceWorktreeJanitorHostedService(
        ISpaceStore store,
        IGitWorktreeService git,
        IOptions<AgentSpaceOptions> options,
        ILogger<SpaceWorktreeJanitorHostedService> logger)
    {
        _store = store;
        _git = git;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Space worktree janitor pass failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var spaces = await _store.ListSpacesAsync(ownerId: null, ct).ConfigureAwait(false);
        var retainBefore = DateTime.UtcNow.AddHours(-_options.WorktreeRetainHours);

        foreach (var space in spaces)
        {
            var members = await _store.ListMembersAsync(space.SpaceId, ct).ConfigureAwait(false);
            foreach (var member in members)
            {
                if (member.Status is not (SpaceMemberStatus.Completed or SpaceMemberStatus.Failed))
                    continue;
                if (member.UpdatedAtUtc > retainBefore)
                    continue;

                try
                {
                    _git.EnsurePathWithinSpace(space.RootPath, member.WorktreePath);
                    await _git.RemoveWorktreeAsync(space.RootPath, member.WorktreePath, force: true, ct).ConfigureAwait(false);
                    var updated = member with { Status = SpaceMemberStatus.Removed, UpdatedAtUtc = DateTime.UtcNow };
                    await _store.UpdateMemberAsync(updated, ct).ConfigureAwait(false);
                    _logger.LogInformation("Removed expired worktree {Member} in space {Space}", member.MemberId, space.SpaceId);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to remove worktree {Member}", member.MemberId);
                }
            }
        }
    }
}
