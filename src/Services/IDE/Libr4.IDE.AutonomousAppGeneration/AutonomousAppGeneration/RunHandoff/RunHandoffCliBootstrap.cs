using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Flow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libr4.IDE.Application.AutonomousAppGeneration.RunHandoff;

public sealed record RunHandoffCliOptions(
    string RunsRoot = ".logs/runs",
    string ExportRoot = ".logs/run-exports",
    string SessionDbPath = ".logs/agent-sessions.db",
    string IdempotencyRoot = ".logs/run-imports",
    int RetentionDays = 7);

public static class RunHandoffCliBootstrap
{
    public static ServiceProvider Build(RunHandoffCliOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        services.Configure<AgentRuntimeOptions>(o =>
        {
            o.RunsRoot = options.RunsRoot;
            o.SessionDbPath = options.SessionDbPath;
        });
        services.Configure<RunExportOptions>(o =>
        {
            o.ExportRootPath = options.ExportRoot;
            o.RetentionDays = options.RetentionDays;
        });
        services.Configure<RunImportOptions>(o => o.IdempotencyRootPath = options.IdempotencyRoot);
        services.Configure<FlowEngineOptions>(o => o.RunsRoot = options.RunsRoot);

        services.AddSingleton<IAppGenerationRepository>(
            _ => new RunHandoffHydratingRepository(options.RunsRoot));
        services.AddSingleton<RunSessionSnapshotExporter>();
        services.AddSingleton<AgentRunPermissionStore>();
        services.AddSingleton<IAgentRunPermissionStore>(sp => sp.GetRequiredService<AgentRunPermissionStore>());
        services.AddSingleton<IFlowProgressStore, FileFlowProgressStore>();
        services.AddRunHandoff(null);

        return services.BuildServiceProvider();
    }
}
