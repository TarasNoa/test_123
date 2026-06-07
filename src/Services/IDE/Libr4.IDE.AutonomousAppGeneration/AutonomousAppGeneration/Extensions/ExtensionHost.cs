using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Extensions;

public interface IExtensionHost
{
    bool Enabled { get; }

    IReadOnlyList<LoadedExtension> Extensions { get; }

    IReadOnlyList<ExtensionHookBinding> Hooks { get; }

    IReadOnlyList<ExtensionToolBinding> Tools { get; }

    IReadOnlyList<ExtensionSkillBinding> Skills { get; }

    Task RefreshAsync(string? workspaceRoot = null, CancellationToken ct = default);
}

public sealed class ExtensionHost : IExtensionHost
{
    private readonly ExtensionHostOptions _options;
    private readonly ILogger<ExtensionHost> _logger;
    private readonly object _lock = new();
    private IReadOnlyList<LoadedExtension> _extensions = Array.Empty<LoadedExtension>();
    private IReadOnlyList<ExtensionHookBinding> _hooks = Array.Empty<ExtensionHookBinding>();
    private IReadOnlyList<ExtensionToolBinding> _tools = Array.Empty<ExtensionToolBinding>();
    private IReadOnlyList<ExtensionSkillBinding> _skills = Array.Empty<ExtensionSkillBinding>();

    public ExtensionHost(IOptions<ExtensionHostOptions> options, ILogger<ExtensionHost> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool Enabled => _options.Enabled;

    public IReadOnlyList<LoadedExtension> Extensions
    {
        get
        {
            lock (_lock)
                return _extensions;
        }
    }

    public IReadOnlyList<ExtensionHookBinding> Hooks
    {
        get
        {
            lock (_lock)
                return _hooks;
        }
    }

    public IReadOnlyList<ExtensionToolBinding> Tools
    {
        get
        {
            lock (_lock)
                return _tools;
        }
    }

    public IReadOnlyList<ExtensionSkillBinding> Skills
    {
        get
        {
            lock (_lock)
                return _skills;
        }
    }

    public Task RefreshAsync(string? workspaceRoot = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!_options.Enabled)
        {
            lock (_lock)
            {
                _extensions = Array.Empty<LoadedExtension>();
                _hooks = Array.Empty<ExtensionHookBinding>();
                _tools = Array.Empty<ExtensionToolBinding>();
                _skills = Array.Empty<ExtensionSkillBinding>();
            }

            return Task.CompletedTask;
        }

        var discovered = new List<LoadedExtension>();
        discovered.AddRange(LoadFromRoot(ResolveUserRoot(), ExtensionSource.User));
        discovered.AddRange(LoadFromRoot(ResolveProjectRoot(workspaceRoot), ExtensionSource.Project));

        var byId = new Dictionary<string, LoadedExtension>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in discovered)
            byId[ext.Id] = ext;

        var hooks = new List<ExtensionHookBinding>();
        var tools = new List<ExtensionToolBinding>();
        var skills = new List<ExtensionSkillBinding>();

        foreach (var extension in byId.Values)
        {
            foreach (var hook in extension.Manifest.Hooks)
            {
                if (string.IsNullOrWhiteSpace(hook.Script))
                    continue;

                try
                {
                    var scriptPath = ExtensionManifestLoader.ResolveRelativePath(extension.RootPath, hook.Script);
                    hooks.Add(new ExtensionHookBinding(extension, hook, scriptPath));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping hook in extension {ExtensionId}", extension.Id);
                }
            }

            foreach (var tool in extension.Manifest.Tools)
            {
                if (string.IsNullOrWhiteSpace(tool.Name) || string.IsNullOrWhiteSpace(tool.Script))
                    continue;

                try
                {
                    var scriptPath = ExtensionManifestLoader.ResolveRelativePath(extension.RootPath, tool.Script);
                    tools.Add(new ExtensionToolBinding(extension, tool, scriptPath));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping tool {Tool} in extension {ExtensionId}", tool.Name, extension.Id);
                }
            }

            foreach (var skill in extension.Manifest.Skills)
            {
                if (string.IsNullOrWhiteSpace(skill.Id) || string.IsNullOrWhiteSpace(skill.Path))
                    continue;

                try
                {
                    var skillPath = ExtensionManifestLoader.ResolveRelativePath(extension.RootPath, skill.Path);
                    skills.Add(new ExtensionSkillBinding(extension, skill, skillPath));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping skill {Skill} in extension {ExtensionId}", skill.Id, extension.Id);
                }
            }
        }

        lock (_lock)
        {
            _extensions = byId.Values.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase).ToList();
            _hooks = hooks;
            _tools = tools;
            _skills = skills;
        }

        _logger.LogInformation(
            "Extension host loaded {ExtensionCount} extensions ({HookCount} hooks, {ToolCount} tools, {SkillCount} skills)",
            _extensions.Count,
            _hooks.Count,
            _tools.Count,
            _skills.Count);

        return Task.CompletedTask;
    }

    private IReadOnlyList<LoadedExtension> LoadFromRoot(string root, ExtensionSource source)
    {
        try
        {
            return ExtensionManifestLoader.ScanDirectory(root, source);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed scanning extensions in {Root}", root);
            return Array.Empty<LoadedExtension>();
        }
    }

    private string ResolveUserRoot()
    {
        if (Path.IsPathRooted(_options.UserExtensionsRoot))
            return _options.UserExtensionsRoot;

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.GetFullPath(Path.Combine(profile, _options.UserExtensionsRoot));
    }

    private string ResolveProjectRoot(string? workspaceRoot)
    {
        if (Path.IsPathRooted(_options.ProjectExtensionsRoot))
            return _options.ProjectExtensionsRoot;

        var baseDir = string.IsNullOrWhiteSpace(workspaceRoot)
            ? Directory.GetCurrentDirectory()
            : workspaceRoot;
        return Path.GetFullPath(Path.Combine(baseDir, _options.ProjectExtensionsRoot));
    }
}

public sealed class ExtensionHostStartup : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly IExtensionHost _host;

    public ExtensionHostStartup(IExtensionHost host) => _host = host;

    public Task StartAsync(CancellationToken cancellationToken) =>
        _host.RefreshAsync(ct: cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class ExtensionLifecycleHookBridge
{
    private readonly IExtensionHost _host;
    private readonly ISandboxedExtensionRunner _runner;
    private readonly ILogger<ExtensionLifecycleHookBridge> _logger;

    public ExtensionLifecycleHookBridge(
        IExtensionHost host,
        ISandboxedExtensionRunner runner,
        ILogger<ExtensionLifecycleHookBridge> logger)
    {
        _host = host;
        _runner = runner;
        _logger = logger;
    }

    public async Task RunForKindAsync(AgentHookKind kind, HookContext context, CancellationToken ct)
    {
        if (!_host.Enabled)
            return;

        foreach (var binding in _host.Hooks.Where(h => TryMapKind(h.Definition.Kind, out var mapped) && mapped == kind))
        {
            var result = await _runner.RunHookAsync(binding, context, ct).ConfigureAwait(false);
            if (!result.Success
                && string.Equals(binding.Definition.OnFailure, "block", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"extension_hook_failed: {binding.Extension.Id}/{binding.Definition.Kind}: {result.Output}");
            }

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Extension hook failed (log-only) extension={ExtensionId} kind={Kind}: {Output}",
                    binding.Extension.Id,
                    binding.Definition.Kind,
                    result.Output);
            }
        }
    }

    private static bool TryMapKind(string raw, out AgentHookKind kind)
    {
        kind = AgentHookKind.SessionStart;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return Enum.TryParse(raw.Trim(), ignoreCase: true, out kind);
    }
}

public sealed class ExtensionLifecycleHookDispatcher : IAgentLifecycleHook
{
    private readonly ExtensionLifecycleHookBridge _bridge;
    private readonly AgentHookKind _kind;

    public ExtensionLifecycleHookDispatcher(ExtensionLifecycleHookBridge bridge, AgentHookKind kind)
    {
        _bridge = bridge;
        _kind = kind;
    }

    public AgentHookKind Kind => _kind;

    public int Order => 900;

    public ValueTask ExecuteAsync(HookContext context, CancellationToken ct) =>
        new(_bridge.RunForKindAsync(_kind, context, ct));
}
