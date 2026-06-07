using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentBackends;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.Runtime;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed record DelegationWorkerCliOptions(string RunsRoot = ".logs/runs");

public static class DelegationWorkerCliBootstrap
{
    public static ServiceProvider Build(DelegationWorkerCliOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.Configure<AgentRuntimeOptions>(o =>
        {
            o.RunsRoot = options.RunsRoot;
            o.SessionDbPath = Path.Combine(options.RunsRoot, "..", "agent-sessions-cli.db");
        });
        services.Configure<DelegationRuntimeOptions>(o => o.UseOutOfProcessWorkers = false);

        services.AddAgentRuntime(null);
        services.AddAgentBackends(null);

        return services.BuildServiceProvider();
    }

    public static ToolContext CreateStubContext(Guid runId, string runsRoot)
    {
        var hostPath = ResolveWorkspacePath(runId, runsRoot);
        Directory.CreateDirectory(hostPath);

        var workspace = new ShadowWorkspaceContext(runId, hostPath, string.Empty, CliNullRuntimeSession.Instance);
        var accessor = new CliShadowWorkspaceAccessor(hostPath);
        return new ToolContext
        {
            Workspace = workspace,
            Accessor = accessor,
            WorkingFiles = new List<GeneratedFile>(),
            FileState = new FileStateCache(),
            Session = new AgentSessionState { RunId = runId },
            ToolInput = JsonDocument.Parse("{}").RootElement
        };
    }

    internal static string ResolveWorkspacePath(Guid runId, string runsRoot)
    {
        var runDir = Path.Combine(runsRoot, runId.ToString("D"));
        var workspace = Path.Combine(runDir, "workspace");
        return Directory.Exists(workspace) ? workspace : runDir;
    }
}

internal sealed class CliShadowWorkspaceAccessor : IShadowWorkspaceAccessor
{
    private readonly string _hostPath;

    public CliShadowWorkspaceAccessor()
        => _hostPath = Directory.GetCurrentDirectory();

    public CliShadowWorkspaceAccessor(string hostPath)
        => _hostPath = hostPath;

    public bool TryGetWorkspace(Guid workspaceId, out ShadowWorkspaceContext context)
    {
        context = new ShadowWorkspaceContext(
            workspaceId,
            _hostPath,
            string.Empty,
            CliNullRuntimeSession.Instance);
        return true;
    }

    public Task<ExecResult> ExecAsync(Guid workspaceId, string command, CancellationToken ct = default) =>
        Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));

    public Task<string> ReadFileAsync(Guid workspaceId, string relativePath, CancellationToken ct = default)
    {
        var abs = Path.Combine(_hostPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllTextAsync(abs, ct);
    }

    public Task WriteFileAsync(Guid workspaceId, string relativePath, string content, CancellationToken ct = default)
    {
        var abs = Path.Combine(_hostPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var dir = Path.GetDirectoryName(abs);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        return File.WriteAllTextAsync(abs, content, ct);
    }

    public IReadOnlyList<string> GlobFiles(Guid workspaceId, string globPattern)
    {
        if (!Directory.Exists(_hostPath))
            return Array.Empty<string>();

        return Directory
            .EnumerateFiles(_hostPath, "*", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(_hostPath, p).Replace('\\', '/'))
            .Where(p => p.Contains(globPattern.Trim('*'), StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}

internal sealed class CliNullRuntimeSession : IRuntimeSession
{
    public static readonly CliNullRuntimeSession Instance = new();

    public string ProviderName => "cli-null";
    public string SessionId => "cli-null";
    public string HostMountPath => Directory.GetCurrentDirectory();
    public string GuestMountPath => "/workspace";
    public string Image => "cli-null";

    public Task<ExecResult> ExecAsync(
        string command,
        string workingSubDirectory,
        IDictionary<string, string>? environmentVariables = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default) =>
        Task.FromResult(new ExecResult(0, TimeSpan.Zero, Array.Empty<ConsoleLogEntry>()));

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
