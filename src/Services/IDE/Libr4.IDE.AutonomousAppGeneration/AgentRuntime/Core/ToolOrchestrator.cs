using System.Diagnostics;
using System.Text.Json;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Abstractions;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Models;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Pathing;
using Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Schema;
using Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libr4.IDE.Application.AutonomousAppGeneration.AgentRuntime.Core;

public sealed class ToolOrchestrator
{
    private readonly IAgentToolRegistry _registry;
    private readonly IPermissionGate _permissions;
    private readonly AgentToolHookPipeline _hooks;
    private readonly IWorkspacePathValidator _pathValidator;
    private readonly IContextInjector? _contextInjector;
    private readonly AgentRuntimeOptions _options;
    private readonly ILogger<ToolOrchestrator> _logger;

    public ToolOrchestrator(
        IAgentToolRegistry registry,
        IPermissionGate permissions,
        AgentToolHookPipeline hooks,
        IWorkspacePathValidator pathValidator,
        IOptions<AgentRuntimeOptions> options,
        ILogger<ToolOrchestrator> logger,
        IContextInjector? contextInjector = null)
    {
        _registry = registry;
        _permissions = permissions;
        _hooks = hooks;
        _pathValidator = pathValidator;
        _contextInjector = contextInjector;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        AgentToolCall call,
        ToolContext context,
        CancellationToken ct)
    {
        if (context.AllowedTools is { Count: > 0 } allowed
            && !allowed.Contains(call.Name, StringComparer.OrdinalIgnoreCase))
        {
            return new ToolExecutionResult(
                call.Name,
                false,
                $"Tool '{call.Name}' not allowed for this subagent (toolset restriction)",
                Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }

        var tool = _registry.TryGet(call.Name);
        if (tool is null)
        {
            return new ToolExecutionResult(
                call.Name,
                false,
                $"Unknown tool '{call.Name}'",
                Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }

        if (_options.EnableStrictToolSchemaValidation)
        {
            var schema = ToolInputValidator.ValidateBeforeExecute(tool.Name, call.Input);
            if (!schema.IsValid)
            {
                return new ToolExecutionResult(
                    call.Name,
                    false,
                    $"schema_validation_failed: {schema.Error}",
                    Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
            }
        }

        var permission = await _permissions.EvaluateAsync(tool, call.Input, context, ct).ConfigureAwait(false);
        if (permission.Kind == PermissionDecisionKind.Deny)
        {
            return new ToolExecutionResult(
                call.Name,
                false,
                $"Permission denied: {permission.Reason}",
                Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }

        if (RequiresPathValidation(tool.Name)
            && call.Input.TryGetProperty("path", out var pathProp)
            && pathProp.ValueKind == JsonValueKind.String)
        {
            var pathValidation = _pathValidator.Validate(
                pathProp.GetString()!,
                new ToolContextPaths(context.Workspace.HostPath, context.Session.RunId));
            if (!pathValidation.Allowed)
            {
                _pathValidator.AuditDenied(pathValidation, tool.Name, context.Session.RunId);
                return new ToolExecutionResult(
                    call.Name,
                    false,
                    $"path_denied: {pathValidation.DenyReason}",
                    Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
            }
        }

        if (_options.EnforceReadBeforeWrite
            && (string.Equals(tool.Name, "edit_file", StringComparison.OrdinalIgnoreCase)
                || string.Equals(tool.Name, "write_file", StringComparison.OrdinalIgnoreCase)))
        {
            var path = call.Input.TryGetProperty("path", out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;
            if (!string.IsNullOrWhiteSpace(path)
                && AgentGenerationPolicy.RequiresReadBeforeWrite(context, tool.Name, path))
            {
                var normalized = FixerPatchScopePolicy.NormalizePatchRelativePath(path);
                var fileExists = AgentGenerationPolicy.WorkspaceFileExists(context, normalized);
                return new ToolExecutionResult(
                    call.Name,
                    false,
                    fileExists
                        ? $"read_file required before modifying existing file '{normalized}' (read-before-write policy)"
                        : $"read_file required before edit_file on '{normalized}'",
                    Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
            }
        }

        var execContext = new ToolContext
        {
            Workspace = context.Workspace,
            Accessor = context.Accessor,
            WorkingFiles = context.WorkingFiles,
            FileState = context.FileState,
            Plan = context.Plan,
            BuildLog = context.BuildLog,
            Mode = context.Mode,
            Session = context.Session,
            ToolInput = call.Input
        };
        context.Session.LastToolInputJson = call.Input.GetRawText();

        try
        {
            await _hooks.RunBeforeAsync(tool, execContext, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return new ToolExecutionResult(
                call.Name,
                false,
                ex.Message,
                Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }

        ToolExecutionResult result;
        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug("Agent tool {Tool} executing in workspace {Ws}", tool.Name, context.Workspace.WorkspaceId);
            result = await tool.ExecuteAsync(call.Input, execContext, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Agent tool {Tool} failed", tool.Name);
            return new ToolExecutionResult(
                call.Name,
                false,
                ex.Message,
                Array.Empty<Domain.AutonomousAppGeneration.GeneratedFile>());
        }

        sw.Stop();
        context.Session.LastToolDurationMs = sw.ElapsedMilliseconds;
        await _hooks.RunAfterAsync(tool, execContext, result, ct).ConfigureAwait(false);
        return AppendJitContextIfNeeded(call, context, result);
    }

    private ToolExecutionResult AppendJitContextIfNeeded(
        AgentToolCall call,
        ToolContext context,
        ToolExecutionResult result)
    {
        if (!result.Success || _contextInjector is null || !TryExtractAccessedPath(call, out var path))
            return result;

        if (!_contextInjector.TryInjectForPath(
                path,
                context.Workspace.HostPath,
                context.WorkingFiles.ToList(),
                out var jit)
            || string.IsNullOrWhiteSpace(jit))
            return result;

        context.Session.LastAccessedRelativePath = path;
        context.Session.ActiveLibr4Context = jit;
        var output = result.Output + "\n\n--- LIBR4 JIT CONTEXT ---\n" + jit;
        return result with { Output = output };
    }

    private static bool TryExtractAccessedPath(AgentToolCall call, out string path)
    {
        path = string.Empty;
        if (!call.Input.TryGetProperty("path", out var pathProp) || pathProp.ValueKind != JsonValueKind.String)
            return false;

        path = FixerPatchScopePolicy.NormalizePatchRelativePath(pathProp.GetString() ?? string.Empty);
        return !string.IsNullOrWhiteSpace(path);
    }

    private static bool RequiresPathValidation(string toolName) =>
        string.Equals(toolName, "read_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "write_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "edit_file", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "apply_patch", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "list_directory", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "grep", StringComparison.OrdinalIgnoreCase)
        || string.Equals(toolName, "glob", StringComparison.OrdinalIgnoreCase);
}
