using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Spaces;

public sealed class AgentSpaceService : IAgentSpaceService
{
    private readonly ISpaceStore _store;
    private readonly IGitWorktreeService _git;
    private readonly ISpaceContextBus _context;
    private readonly AgentSpaceOptions _options;
    private readonly ILogger<AgentSpaceService> _logger;

    public AgentSpaceService(
        ISpaceStore store,
        IGitWorktreeService git,
        ISpaceContextBus context,
        IOptions<AgentSpaceOptions> options,
        ILogger<AgentSpaceService> logger)
    {
        _store = store;
        _git = git;
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentSpace> CreateSpaceAsync(CreateSpaceRequest request, CancellationToken ct = default)
    {
        var spaceId = Guid.NewGuid();
        var root = Path.Combine(Path.GetFullPath(_options.SpacesRoot), spaceId.ToString("D"));
        var baseBranch = string.IsNullOrWhiteSpace(request.BaseBranch) ? _options.DefaultBaseBranch : request.BaseBranch!;
        var owner = string.IsNullOrWhiteSpace(request.OwnerId) ? "default" : request.OwnerId!;
        var scope = _context.BuildHermesScope(spaceId);

        Directory.CreateDirectory(root);
        var mainPath = await _git.InitOrCloneMainWorktreeAsync(root, request.RepositoryUrl, baseBranch, ct).ConfigureAwait(false);

        var integrationBranch = _options.IntegrationBranchName;
        try
        {
            await RunGitInMainAsync(mainPath, ct, "branch", integrationBranch).ConfigureAwait(false);
        }
        catch
        {
            // branch may already exist on clone
        }

        var sharedDir = Path.Combine(root, "shared");
        Directory.CreateDirectory(sharedDir);
        var libr4Md = Path.Combine(sharedDir, "LIBR4.md");
        if (!File.Exists(libr4Md))
        {
            await File.WriteAllTextAsync(
                libr4Md,
                $"# {request.Name}\n\n{(request.UserRequest ?? "Agent space")}\n",
                ct).ConfigureAwait(false);
        }

        var space = new AgentSpace(
            SpaceId: spaceId,
            Name: request.Name,
            RepositoryUrl: request.RepositoryUrl,
            BaseBranch: baseBranch,
            OwnerId: owner,
            SharedMemoryScope: scope,
            McpProfile: request.McpProfile,
            CreatedAtUtc: DateTime.UtcNow,
            RootPath: root,
            IntegrationBranch: integrationBranch);

        await _store.InsertSpaceAsync(space, ct).ConfigureAwait(false);
        await _context.PublishAsync(spaceId, "space_created", request.Name, request.UserRequest, ct: ct).ConfigureAwait(false);
        _logger.LogInformation("Created agent space {SpaceId} main={MainPath}", spaceId, mainPath);
        return space;
    }

    public async Task<AgentSpaceDetail?> GetSpaceDetailAsync(Guid spaceId, CancellationToken ct = default)
    {
        var space = await _store.GetSpaceAsync(spaceId, ct).ConfigureAwait(false);
        if (space is null)
            return null;

        var members = await _store.ListMembersAsync(spaceId, ct).ConfigureAwait(false);
        var context = await _context.ReadRecentAsync(spaceId, 32, ct).ConfigureAwait(false);
        return new AgentSpaceDetail(space, members, context);
    }

    public Task<IReadOnlyList<AgentSpace>> ListSpacesAsync(string? ownerId, CancellationToken ct = default) =>
        _store.ListSpacesAsync(ownerId, ct);

    public async Task<SpaceMember> SpawnAgentAsync(Guid spaceId, SpawnSpaceAgentRequest request, CancellationToken ct = default)
    {
        var space = await _store.GetSpaceAsync(spaceId, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException("space_not_found");

        var active = await _store.CountActiveMembersAsync(spaceId, ct).ConfigureAwait(false);
        if (active >= _options.MaxWorktreesPerSpace)
            throw new InvalidOperationException("space_worktree_limit_reached");

        var total = (await _store.ListMembersAsync(spaceId, ct).ConfigureAwait(false))
            .Count(m => m.Status != SpaceMemberStatus.Removed);
        if (total >= _options.HardWorktreeCap)
            throw new InvalidOperationException("space_worktree_hard_cap_reached");

        var memberId = Guid.NewGuid().ToString("N")[..8];
        var roleSlug = request.Role.ToString().ToLowerInvariant();
        var branchName = request.BindToIntegrationWorktree && request.Role == SpaceMemberRole.Verifier
            ? space.IntegrationBranch
            : $"agent/{roleSlug}/{memberId}";
        var mainPath = Path.Combine(space.RootPath, "main");

        string worktreePath;
        if (request.BindToIntegrationWorktree && request.Role == SpaceMemberRole.Verifier)
        {
            worktreePath = mainPath;
            _git.EnsurePathWithinSpace(space.RootPath, worktreePath);
        }
        else
        {
            var wt = await _git.AddAgentWorktreeAsync(space.RootPath, mainPath, memberId, request.Role, branchName, ct).ConfigureAwait(false);
            worktreePath = wt.Path;
        }

        var now = DateTime.UtcNow;
        var member = new SpaceMember(
            MemberId: memberId,
            SpaceId: spaceId,
            Role: request.Role,
            RunId: request.RunId,
            WorktreePath: worktreePath,
            BranchName: branchName,
            Status: SpaceMemberStatus.Running,
            CreatedAtUtc: now,
            UpdatedAtUtc: now,
            LastError: null);

        await _store.InsertMemberAsync(member, ct).ConfigureAwait(false);
        await _context.PublishAsync(
            spaceId,
            "member_spawned",
            $"Spawned {request.Role}",
            request.Task ?? worktreePath,
            memberId,
            ct).ConfigureAwait(false);

        return member;
    }

    public async Task<MergeSpaceMemberResult> MergeMemberAsync(Guid spaceId, string memberId, CancellationToken ct = default)
    {
        var space = await _store.GetSpaceAsync(spaceId, ct).ConfigureAwait(false)
                    ?? throw new KeyNotFoundException("space_not_found");
        var member = await _store.GetMemberAsync(spaceId, memberId, ct).ConfigureAwait(false)
                     ?? throw new KeyNotFoundException("member_not_found");

        var mainPath = Path.Combine(space.RootPath, "main");
        var report = await _git.MergeBranchAsync(mainPath, member.BranchName, space.IntegrationBranch, ct).ConfigureAwait(false);

        if (SpaceMergeConflictPolicy.RequiresHumanResolution(report.Success, report.Conflicts))
        {
            var humanReport = SpaceMergeConflictPolicy.FormatHumanReport(report.Conflicts);
            await _context.PublishAsync(
                spaceId,
                SpaceMergeConflictPolicy.HumanPromptRequiredReason,
                $"Merge {member.BranchName}",
                humanReport,
                memberId,
                ct).ConfigureAwait(false);

            return new MergeSpaceMemberResult(
                false,
                humanReport,
                report.Conflicts,
                space.IntegrationBranch);
        }

        await _context.PublishAsync(
            spaceId,
            report.Success ? "merge_completed" : "merge_conflict",
            $"Merge {member.BranchName}",
            report.Output,
            memberId,
            ct).ConfigureAwait(false);

        if (report.Success)
        {
            var updated = member with { Status = SpaceMemberStatus.Completed, UpdatedAtUtc = DateTime.UtcNow };
            await _store.UpdateMemberAsync(updated, ct).ConfigureAwait(false);
        }

        return new MergeSpaceMemberResult(
            report.Success,
            report.Output,
            report.Conflicts,
            space.IntegrationBranch);
    }

    public async Task<WorktreeDirectoryListing?> ListWorktreeFilesAsync(
        Guid spaceId,
        string memberId,
        string? relativePath = null,
        CancellationToken ct = default)
    {
        var space = await _store.GetSpaceAsync(spaceId, ct).ConfigureAwait(false);
        if (space is null)
            return null;

        var member = await _store.GetMemberAsync(spaceId, memberId, ct).ConfigureAwait(false);
        if (member is null)
            return null;

        var worktreeRoot = Path.GetFullPath(member.WorktreePath);
        _git.EnsurePathWithinSpace(space.RootPath, worktreeRoot);

        var rel = NormalizeRelativePath(relativePath);
        var targetDir = string.IsNullOrEmpty(rel)
            ? worktreeRoot
            : Path.GetFullPath(Path.Combine(worktreeRoot, rel.Replace('/', Path.DirectorySeparatorChar)));

        _git.EnsurePathWithinSpace(space.RootPath, targetDir);
        if (!Directory.Exists(targetDir))
            return null;

        var entries = new List<WorktreeFileEntry>();
        foreach (var entry in Directory.EnumerateFileSystemEntries(targetDir).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(entry);
            if (name is ".git" or ".gitignore")
                continue;

            var entryRel = string.IsNullOrEmpty(rel)
                ? name
                : $"{rel}/{name}";
            if (Directory.Exists(entry))
            {
                entries.Add(new WorktreeFileEntry(name, entryRel.Replace('\\', '/'), true, null));
                continue;
            }

            var info = new FileInfo(entry);
            entries.Add(new WorktreeFileEntry(name, entryRel.Replace('\\', '/'), false, info.Length));
        }

        return new WorktreeDirectoryListing(worktreeRoot, rel, entries);
    }

    public async Task<GitMergePreview?> PreviewMergeAsync(Guid spaceId, string memberId, CancellationToken ct = default)
    {
        var space = await _store.GetSpaceAsync(spaceId, ct).ConfigureAwait(false);
        if (space is null)
            return null;

        var member = await _store.GetMemberAsync(spaceId, memberId, ct).ConfigureAwait(false);
        if (member is null)
            return null;

        var mainPath = Path.Combine(space.RootPath, "main");
        return await _git.PreviewMergeAsync(mainPath, member.BranchName, space.IntegrationBranch, ct: ct)
            .ConfigureAwait(false);
    }

    private static string NormalizeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        var normalized = relativePath.Replace('\\', '/').Trim().TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("path_traversal_denied");

        return normalized;
    }

    private static async Task RunGitInMainAsync(string mainPath, CancellationToken ct, params string[] args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = mainPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = System.Diagnostics.Process.Start(psi)
                            ?? throw new InvalidOperationException("git_not_available");
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git branch failed: {process.ExitCode}");
    }
}
