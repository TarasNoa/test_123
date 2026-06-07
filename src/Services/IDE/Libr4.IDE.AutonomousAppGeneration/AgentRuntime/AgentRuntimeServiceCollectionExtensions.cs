using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Api;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Events;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.ExecPolicy;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Hooks;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Patching;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Persistence;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Playbook;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Delegation;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.DMail;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.SlashCommands;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Prompting.Templates;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Rollout;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Services;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Skills;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Subagents;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Tools.Browser;
using Libr4.IDE.Application.Obscura;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Compaction;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Fragments;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;

public static class AgentRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddAgentRuntime(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<AgentRuntimeOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentRuntime"));
            services.Configure<Libr4ContextOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentRuntime:Libr4Context"));
            services.Configure<AgentSpecOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentRuntime"));
            services.Configure<ConfigurableScriptHookOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentHooks"));
            services.Configure<SkillActivationOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentRuntime:SkillActivation"));
            services.Configure<ContextFragmentOptions>(
                configuration.GetSection("AutonomousAppGeneration:ContextFragments"));
            services.Configure<SemanticCompactionOptions>(
                configuration.GetSection("AutonomousAppGeneration:SemanticCompaction"));
            services.Configure<PromptTemplateOptions>(
                configuration.GetSection("AutonomousAppGeneration:PromptTemplates"));
            services.Configure<RepairPlaybookOptions>(
                configuration.GetSection("AutonomousAppGeneration:AgentRuntime:RepairPlaybook"));
            services.Configure<DelegationRuntimeOptions>(
                configuration.GetSection(DelegationRuntimeOptions.SectionName));
        }
        else
        {
            services.Configure<AgentRuntimeOptions>(_ => { });
            services.Configure<Libr4ContextOptions>(_ => { });
            services.Configure<AgentSpecOptions>(_ => { });
            services.Configure<ConfigurableScriptHookOptions>(_ => { });
            services.Configure<SkillActivationOptions>(_ => { });
            services.Configure<ContextFragmentOptions>(_ => { });
            services.Configure<SemanticCompactionOptions>(_ => { });
            services.Configure<PromptTemplateOptions>(_ => { });
            services.Configure<RepairPlaybookOptions>(_ => { });
            services.Configure<DelegationRuntimeOptions>(_ => { });
        }

        services.AddSingleton<SqliteRepairPlaybookStore>();
        services.AddSingleton<IRepairPlaybookStore>(sp => sp.GetRequiredService<SqliteRepairPlaybookStore>());
        services.AddHostedService<RepairPlaybookSchemaMigrator>();

        services.AddSingleton<PromptVariantSelector>();
        services.AddSingleton<IPromptTemplateRegistry, PromptTemplateRegistry>();

        services.AddSingleton<HeuristicSemanticCompactor>();
        services.AddSingleton<LlmSemanticCompactor>();
        services.AddSingleton<ISemanticCompactor>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SemanticCompactionOptions>>().Value;
            return opts.UseLlmSummarizer
                ? sp.GetRequiredService<LlmSemanticCompactor>()
                : sp.GetRequiredService<HeuristicSemanticCompactor>();
        });
        services.AddSingleton<IContextCompactor, SemanticContextCompactor>();

        services.AddTransient<IContextFragmentManager, ContextFragmentManager>();
        services.AddTransient<IContextFragmentRepairAssembler, ContextFragmentRepairAssembler>();

        services.AddSingleton<ISkillManifestRegistry, FileSkillManifestRegistry>();
        services.AddSingleton<ISkillConsentGate, InMemorySkillConsentGate>();
        services.AddSingleton<IBuiltinPromptVarResolver, BuiltinPromptVarResolver>();
        services.AddSingleton<IContextInjector, Libr4ContextInjector>();
        services.AddSingleton<IAgentSpecRegistry, AgentSpecRegistry>();
        services.AddSingleton<ISubagentStore, FileSubagentStore>();
        services.AddSingleton<IDelegationManager, FileDelegationManager>();
        services.AddSingleton<IBackgroundFleetScheduler, BackgroundFleetScheduler>();
        services.AddHostedService<DelegationTimeoutAlertMonitor>();
        services.AddSingleton<ManagedDelegationWorkerHost>();
        services.AddSingleton<IDelegationWorkerHost, ProcessDelegationWorkerHost>();
        services.AddSingleton<IDelegationExploreRunner, DelegationExploreRunner>();
        services.AddSingleton<IDMailBus, FileDMailBus>();
        services.AddSingleton<IFeatureBatchHandoffCoordinator, FeatureBatchHandoffCoordinator>();
        services.AddSingleton<ISlashCommandRegistry, SlashCommandRegistry>();
        services.AddOptions<DMailOptions>().Configure<IOptions<AgentRuntimeOptions>>((d, runtime) =>
            d.RunsRoot = runtime.Value.RunsRoot);
        services.AddSingleton<SubagentExecutionService>();
        services.AddScoped<IAgentSpecSubagentRunner, AgentSpecSubagentRunner>();

        services.AddSingleton<IFileStateCache, FileStateCache>();
        services.AddSingleton<IPathAccessAudit, InMemoryPathAccessAudit>();
        services.AddSingleton<IWorkspacePathValidator, WorkspacePathValidator>();
        services.AddSingleton<IAgentRunPermissionStore, AgentRunPermissionStore>();
        services.AddSingleton<IExecPolicyEngine, YamlExecPolicyEngine>();
        services.AddSingleton<IExecPolicyJsonlAudit, ExecPolicyJsonlAudit>();
        services.AddSingleton<IObscuraExecPolicyEngine, YamlObscuraExecPolicyEngine>();
        services.AddSingleton<IObscuraExecPolicyJsonlAudit, ObscuraExecPolicyJsonlAudit>();
        services.AddSingleton<IPatchAttemptRecorder, PatchAttemptRecorder>();
        services.AddSingleton<IAgentRuntimeEventHub, AgentRuntimeEventHub>();
        services.AddSingleton<INdjsonEventWriter, NdjsonEventWriter>();
        services.AddSingleton<IAgentLifecycleHookRunner, AgentLifecycleHookRunner>();
        services.AddSingleton<IRolloutRecorder, FileRolloutRecorder>();
        services.AddSingleton<IRolloutReplayService>(sp => (FileRolloutRecorder)sp.GetRequiredService<IRolloutRecorder>());
        services.AddSingleton<IRunUsageRollupService, RunUsageRollupService>();
        services.AddSingleton<IAgentSessionStore, SqliteAgentSessionStore>();
        services.AddSingleton<IAgentSessionResumeService>(sp => (SqliteAgentSessionStore)sp.GetRequiredService<IAgentSessionStore>());
        services.AddHostedService<AgentSessionSchemaMigrator>();
        services.AddSingleton<AgentRuntimeStreamService>();

        services.AddSingleton<IPermissionGate, DefaultPermissionGate>();
        services.AddSingleton<RepairPlaybookService>();

        services.AddSingleton<IAgentTool, ReadFileTool>();
        services.AddSingleton<IAgentTool, EditFileTool>();
        services.AddSingleton<IAgentTool, WriteFileTool>();
        services.AddSingleton<IAgentTool, ApplyPatchTool>();
        services.AddSingleton<IAgentTool, ListDirectoryTool>();
        services.AddSingleton<IAgentTool, BashTool>();
        services.AddSingleton<IAgentTool, GrepTool>();
        services.AddSingleton<IAgentTool, SearchCodebaseTool>();
        services.AddSingleton<IAgentTool, GetSymbolContextTool>();
        services.AddSingleton<IAgentTool, GlobTool>();
        services.AddSingleton<IAgentTool, InspectEnvironmentTool>();
        services.AddSingleton<IAgentTool, RunBuildTool>();
        services.AddSingleton<IAgentTool, RunTestsTool>();
        services.AddSingleton<IAgentTool, ToolSearchTool>();
        services.AddSingleton<IAgentTool, TodoWriteTool>();
        services.AddSingleton<IAgentTool, EnterPlanModeTool>();
        services.AddSingleton<IAgentTool, ExitPlanModeTool>();
        services.AddSingleton<IAgentTool, CheckpointTool>();
        services.AddSingleton<IAgentTool, RewindToTagTool>();
        services.AddSingleton<ActivateSkillTool>();
        services.AddSingleton<IAgentTool>(sp => sp.GetRequiredService<ActivateSkillTool>());
        services.AddSingleton<IAgentTool, MemoryWriteTool>();
        services.AddSingleton<IAgentTool, MemoryReadTool>();
        services.AddSingleton<IAgentTool, HonchoProfileTool>();
        services.AddSingleton<IAgentTool, HonchoReasoningTool>();
        services.AddSingleton<IAgentTool, SearchWebTool>();
        services.AddSingleton<IAgentTool, SearchXTool>();
        services.AddSingleton<IAgentTool, SkillAgentTool>();
        services.AddSingleton<IAgentTool, McpAgentTool>();
        services.AddSingleton<IAgentTool, SubagentTool>();
        services.AddSingleton<IAgentTool, TaskTool>();
        services.AddSingleton<IAgentTool, DelegateTool>();
        services.AddSingleton<IAgentTool, DelegationListTool>();
        services.AddSingleton<IAgentTool, DelegationReadTool>();
        services.AddSingleton<IAgentTool, DMailSendTool>();
        services.AddSingleton<IAgentTool, DMailReadTool>();
        services.AddSingleton<IAgentTool, DMailAckTool>();

        services.AddObscuraBrowserPlane(configuration);
        services.AddSingleton<ObscuraBrowserToolFacade>();
        services.AddSingleton<IAgentTool, BrowserLaunchTool>();
        services.AddSingleton<IAgentTool, BrowserNavigateTool>();
        services.AddSingleton<IAgentTool, BrowserSnapshotTool>();
        services.AddSingleton<IAgentTool, BrowserClickTool>();
        services.AddSingleton<IAgentTool, BrowserTypeTool>();
        services.AddSingleton<IAgentTool, BrowserScrollTool>();
        services.AddSingleton<IAgentTool, BrowserWaitTool>();
        services.AddSingleton<IAgentTool, BrowserScreenshotTool>();
        services.AddSingleton<IAgentTool, BrowserExecuteJsTool>();
        services.AddSingleton<IAgentTool, BrowserConsoleTool>();
        services.AddSingleton<IAgentTool, BrowserGetContentTool>();
        services.AddSingleton<IAgentTool, BrowserExtractTool>();
        services.AddSingleton<IAgentTool, BrowserRecordStartTool>();
        services.AddSingleton<IAgentTool, BrowserRecordStopTool>();
        services.AddSingleton<IAgentTool, BrowserCloseTool>();
        services.AddSingleton<IAgentTool, BrowserResearchTool>();

        services.AddSingleton<IAgentToolRegistry, AgentToolRegistry>();

        services.AddSingleton<IAgentToolHook, ObscuraExecPolicyToolHook>();
        services.AddSingleton<IAgentToolHook, ExecPolicyToolHook>();
        services.AddSingleton<IAgentToolHook, ConfigurableScriptHook>();
        services.AddSingleton<IAgentToolHook, PlanModeToolHook>();
        services.AddSingleton<IAgentToolHook, RepairPlaybookToolHook>();
        services.AddSingleton<IAgentToolHook, MemoryPrefetchToolHook>();
        services.AddSingleton<IAgentToolHook, RolloutAuditToolHook>();
        services.AddSingleton<IAgentToolHook, BrowserToolEventHook>();
        services.AddSingleton<IAgentToolHook, EvidenceCaptureToolHook>();
        services.AddSingleton<IAgentToolHook, AgentToolAuditHook>();
        services.AddSingleton<AgentToolHookPipeline>();

        services.AddSingleton<GenerationWorkspaceStore>();
        services.AddSingleton<GenerationWorkspaceAccessor>();

        services.AddScoped<ToolOrchestrator>();
        services.AddScoped<IAgentSession, AgentSession>();
        services.AddScoped<IShadowAgentRepairService, ShadowAgentRepairService>();
        services.AddScoped<IAgentRuntimeIncrementalGenerator, AgentRuntimeIncrementalGenerator>();

        return services;
    }
}
