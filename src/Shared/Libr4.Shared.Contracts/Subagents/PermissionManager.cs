namespace Libr4.Shared.Contracts.Subagents;

/// <summary>
/// Permission context for tool execution.
/// </summary>
public record PermissionContext
{
    /// <summary>
    /// Current permission mode.
    /// </summary>
    public AgentPermissionMode Mode { get; init; } = AgentPermissionMode.Default;

    /// <summary>
    /// Whether safe mode is enabled globally.
    /// </summary>
    public bool IsSafeMode { get; init; }

    /// <summary>
    /// Whether bypass permissions mode is available.
    /// </summary>
    public bool IsBypassPermissionsModeAvailable { get; init; }
}

/// <summary>
/// Permission check result.
/// </summary>
public record PermissionCheckResult
{
    /// <summary>
    /// Whether the operation is allowed.
    /// </summary>
    public bool IsAllowed { get; init; }

    /// <summary>
    /// Reason for denial (if denied).
    /// </summary>
    public string? DenialReason { get; init; }

    /// <summary>
    /// Whether user approval is required.
    /// </summary>
    public bool RequiresApproval { get; init; }
}

/// <summary>
/// Manages permission modes and checks.
/// </summary>
public class PermissionManager
{
    /// <summary>
    /// Checks if a tool operation is allowed based on the permission context.
    /// </summary>
    /// <param name="context">Permission context.</param>
    /// <param name="toolName">Name of the tool being used.</param>
    /// <param name="isWriteOperation">Whether this is a write operation.</param>
    /// <param name="isBashOperation">Whether this is a bash/shell operation.</param>
    /// <returns>Permission check result.</returns>
    public PermissionCheckResult CheckPermission(
        PermissionContext context,
        string toolName,
        bool isWriteOperation = false,
        bool isBashOperation = false)
    {
        // Handle bypass permissions mode
        if (context.Mode == AgentPermissionMode.BypassPermissions)
        {
            if (!context.IsSafeMode && context.IsBypassPermissionsModeAvailable)
            {
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiresApproval = false
                };
            }
            else
            {
                // Fallback to default if safe mode is enabled or bypass not available
                context = context with { Mode = AgentPermissionMode.Default };
            }
        }

        // Handle plan mode (read-only)
        if (context.Mode == AgentPermissionMode.Plan)
        {
            if (isWriteOperation || isBashOperation)
            {
                return new PermissionCheckResult
                {
                    IsAllowed = false,
                    DenialReason = "Plan mode only allows read-only operations",
                    RequiresApproval = false
                };
            }
            return new PermissionCheckResult { IsAllowed = true, RequiresApproval = false };
        }

        // Handle acceptEdits mode
        if (context.Mode == AgentPermissionMode.AcceptEdits)
        {
            if (isWriteOperation)
            {
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiresApproval = false
                };
            }
            // Bash operations still require approval in acceptEdits mode
            if (isBashOperation)
            {
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiresApproval = true
                };
            }
            return new PermissionCheckResult { IsAllowed = true, RequiresApproval = false };
        }

        // Handle dontAsk mode
        if (context.Mode == AgentPermissionMode.DontAsk)
        {
            return new PermissionCheckResult
            {
                IsAllowed = true,
                RequiresApproval = false
            };
        }

        // Handle delegate mode (use parent's mode - handled by caller)
        if (context.Mode == AgentPermissionMode.Delegate)
        {
            return new PermissionCheckResult
            {
                IsAllowed = true,
                RequiresApproval = false
            };
        }

        // Default mode
        if (context.IsSafeMode)
        {
            // In safe mode, require approval for write and bash operations
            if (isWriteOperation || isBashOperation)
            {
                return new PermissionCheckResult
                {
                    IsAllowed = true,
                    RequiresApproval = true
                };
            }
            return new PermissionCheckResult { IsAllowed = true, RequiresApproval = false };
        }

        // YOLO mode (safe mode disabled) - allow everything
        return new PermissionCheckResult
        {
            IsAllowed = true,
            RequiresApproval = false
        };
    }

    /// <summary>
    /// Applies agent permission mode to base context.
    /// </summary>
    /// <param name="baseContext">Base permission context.</param>
    /// <param name="agentPermissionMode">Agent's permission mode.</param>
    /// <param name="isSafeMode">Whether safe mode is enabled.</param>
    /// <returns>Updated permission context.</returns>
    public PermissionContext ApplyAgentPermissionMode(
        PermissionContext baseContext,
        AgentPermissionMode? agentPermissionMode,
        bool isSafeMode)
    {
        if (!agentPermissionMode.HasValue)
            return baseContext;

        // Handle bypass permissions mode
        if (agentPermissionMode == AgentPermissionMode.BypassPermissions)
        {
            if (isSafeMode || !baseContext.IsBypassPermissionsModeAvailable)
            {
                // Fallback to default if safe mode or bypass not available
                return baseContext with { Mode = AgentPermissionMode.Default };
            }
        }

        return baseContext with { Mode = agentPermissionMode.Value };
    }

    /// <summary>
    /// Validates if a permission mode is valid.
    /// </summary>
    /// <param name="mode">Permission mode to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValidPermissionMode(AgentPermissionMode mode)
    {
        return mode switch
        {
            AgentPermissionMode.Default => true,
            AgentPermissionMode.AcceptEdits => true,
            AgentPermissionMode.Plan => true,
            AgentPermissionMode.BypassPermissions => true,
            AgentPermissionMode.DontAsk => true,
            AgentPermissionMode.Delegate => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets the default permission mode for an agent based on its sandbox mode.
    /// </summary>
    /// <param name="sandboxMode">Sandbox mode.</param>
    /// <returns>Recommended permission mode.</returns>
    public AgentPermissionMode GetDefaultPermissionModeForSandboxMode(SandboxMode sandboxMode)
    {
        return sandboxMode switch
        {
            SandboxMode.ReadOnly => AgentPermissionMode.Plan,
            SandboxMode.ReadWrite => AgentPermissionMode.AcceptEdits,
            SandboxMode.FullAccess => AgentPermissionMode.Default,
            _ => AgentPermissionMode.Default
        };
    }
}
