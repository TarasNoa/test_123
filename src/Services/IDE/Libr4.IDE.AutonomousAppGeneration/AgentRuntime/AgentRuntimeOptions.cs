using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Permissions;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime;

public sealed class AgentRuntimeOptions
{
    /// <summary>Primary repair path: Claude Code-style tool loop in shadow workspace.</summary>
    public bool UseAgentRuntimeRepair { get; set; } = true;

    /// <summary>Max LLM↔tool turns per repair iteration.</summary>
    public int MaxTurnsPerRepair { get; set; } = 20;

    /// <summary>Max concurrent read-only tool executions.</summary>
    public int MaxConcurrentReadOnlyTools { get; set; } = 4;

    /// <summary>Truncate individual tool results beyond this char count.</summary>
    public int MaxToolResultChars { get; set; } = 12_000;

    /// <summary>Compact conversation when estimated chars exceed this budget.</summary>
    public int ConversationCharBudget { get; set; } = 48_000;

    /// <summary>Require read_file before edit_file/write_file on a path.</summary>
    public bool EnforceReadBeforeWrite { get; set; } = true;

    /// <summary>Max file size for read_file (bytes).</summary>
    public int MaxReadFileBytes { get; set; } = 512_000;

    /// <summary>Default bash command timeout seconds.</summary>
    public int BashTimeoutSeconds { get; set; } = 300;

    /// <summary>Generate files via agent tool loop instead of batch JSON LLM.</summary>
    public bool UseAgentRuntimeGeneration { get; set; } = true;

    /// <summary>Max LLM↔tool turns per single file generation task.</summary>
    public int MaxTurnsPerGenerationFile { get; set; } = 20;

    /// <summary>Consecutive read-only tools before nudging agent to write_file.</summary>
    public int MaxInvestigationToolsBeforeWriteNudge { get; set; } = 3;

    /// <summary>LLM call retries per agent turn (OpenRouter stream drops).</summary>
    public int LlmRetryAttempts { get; set; } = 3;

    /// <summary>Coerce raw source-code LLM output into write_file during generation.</summary>
    public bool EnableRawContentCoercion { get; set; } = true;

    /// <summary>Escalating recovery when LLM output fails JSON parse (compressed prompt → strict schema → boilerplate).</summary>
    public bool EnableToolCallRecovery { get; set; } = true;

    /// <summary>Apply BoilerplateRegistry templates after repeated invalid JSON (saves turns on manage.py, wsgi, etc.).</summary>
    public bool EnableBoilerplateFallback { get; set; } = true;

    /// <summary>Allow bash during generation (off by default — use read/write/grep/glob).</summary>
    public bool AllowBashDuringGeneration { get; set; } = false;

    public int MaxSubagentDepth { get; set; } = 1;

    public bool EnableMcpToolsInAgentLoop { get; set; } = true;

    public bool EnableSkillToolsInAgentLoop { get; set; } = true;

    public bool EnableSubagentTool { get; set; } = true;

    public bool EnablePlanModeTools { get; set; } = true;

    public bool EnableRepairPlaybook { get; set; } = true;

    public bool EnableSessionPersistence { get; set; } = true;

    public string SessionDbPath { get; set; } = ".logs/agent-runtime/sessions.db";

    public string RolloutDbPath { get; set; } = ".logs/agent-runtime/rollout.db";

    public string RunsRoot { get; set; } = ".logs/runs";

    public string ExecPolicyPath { get; set; } = "AgentRuntime/Config/execpolicy.yaml";

    public string ObscuraExecPolicyPath { get; set; } = "AgentRuntime/Config/obscura-exec-policy.yaml";

    public bool EnableRolloutRecorder { get; set; } = true;

    public bool EnableNdjsonEvents { get; set; } = true;

    public bool EnableStrictToolSchemaValidation { get; set; } = true;

    public bool IncludeReasoningInContext { get; set; } = false;

    public bool EnableEvidenceCaptureHook { get; set; } = true;

    public bool AllowSymlinks { get; set; } = false;

    public string[] DeniedPathPatterns { get; set; } =
    [
        "/etc/*",
        "C:\\Windows\\*"
    ];

    public AgentPermissionMode DefaultPermissionMode { get; set; } = AgentPermissionMode.BypassPermissions;

    public string RolloutDbConnectionString =>
        new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = RolloutDbPath }.ConnectionString;
}
