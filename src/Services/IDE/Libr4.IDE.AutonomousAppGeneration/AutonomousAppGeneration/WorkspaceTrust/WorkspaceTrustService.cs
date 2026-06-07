using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.WorkspaceTrust;

public sealed class WorkspaceTrustService : IWorkspaceTrustService
{
    private readonly IWorkspaceTrustStore _store;
    private readonly WorkspaceTrustOptions _options;
    private readonly ILogger<WorkspaceTrustService> _logger;

    public WorkspaceTrustService(
        IWorkspaceTrustStore store,
        IOptions<WorkspaceTrustOptions> options,
        ILogger<WorkspaceTrustService> logger)
    {
        _store = store;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WorkspaceTrustResolution> ResolveAsync(string workspaceHash, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return Ready(
                workspaceHash,
                _options.DefaultSandboxPolicy,
                _options.DefaultHostMode,
                fromStore: false,
                fromConfigOverride: true);
        }

        if (_options.BypassTrustStore
            || _options.TryGetForcedSandboxPolicy(out var forcedSandbox)
            || _options.TryGetForcedHostMode(out var forcedHost))
        {
            var sandbox = _options.TryGetForcedSandboxPolicy(out var fs)
                ? fs
                : _options.DefaultSandboxPolicy;
            var host = _options.TryGetForcedHostMode(out var fh)
                ? fh
                : _options.DefaultHostMode;

            return Ready(workspaceHash, sandbox, host, fromStore: false, fromConfigOverride: true);
        }

        var stored = await _store.GetAsync(workspaceHash, ct).ConfigureAwait(false);
        if (stored is not null)
        {
            return Ready(
                workspaceHash,
                stored.SandboxPolicy,
                stored.HostMode,
                fromStore: true,
                fromConfigOverride: false);
        }

        var prompt = new WorkspaceTrustPrompt(
            Guid.NewGuid().ToString("D"),
            workspaceHash,
            _options.DefaultSandboxPolicy,
            _options.DefaultHostMode,
            "Choose sandbox and inference policy for this workspace. Your choice can be remembered per project.",
            DateTime.UtcNow);

        _logger.LogInformation("Workspace trust first-run prompt required for hash {Hash}", workspaceHash);
        return new WorkspaceTrustResolution
        {
            NeedsFirstRunPrompt = true,
            Prompt = prompt
        };
    }

    public Task RememberAsync(WorkspaceTrustRecord record, CancellationToken ct = default) =>
        _store.UpsertAsync(record, ct);

    private static WorkspaceTrustResolution Ready(
        string workspaceHash,
        WorkspaceSandboxPolicy sandbox,
        WorkspaceHostMode host,
        bool fromStore,
        bool fromConfigOverride) =>
        new()
        {
            NeedsFirstRunPrompt = false,
            Decision = new WorkspaceTrustDecision(
                workspaceHash,
                sandbox,
                host,
                fromStore,
                fromConfigOverride,
                WorkspaceTrustPolicyMapper.DeniesCloudInference(host))
        };
}
