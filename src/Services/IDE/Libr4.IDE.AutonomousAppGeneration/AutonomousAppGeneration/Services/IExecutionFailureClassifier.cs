using Libr4.IDE.Domain.AutonomousAppGeneration;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Services;

/// <summary>
/// Classifies an <see cref="ExecutionResult"/> + extracted <see cref="ErrorReport"/>s
/// into actionability buckets used by the orchestrator (retry / abort / patch).
///
/// P1-2 of the audit roadmap (see AUDIT_FINDINGS_AND_REMEDIATION_ROADMAP.md):
/// substring heuristics scattered in <see cref="StartAppGenerationCommandHandler"/>
/// were error-prone and could mask real bugs (or over-retry on application logs).
/// This abstraction centralises the rules so:
///   1) the orchestrator stays free of `string.Contains("timeout")` calls;
///   2) the rules are unit-testable in isolation;
///   3) we can later swap in a richer classifier (regex/exit-code/ML).
/// </summary>
public interface IExecutionFailureClassifier
{
    /// <summary>
    /// True when the failure is transient/infrastructure-induced and worth retrying.
    /// Retries should NOT be issued for deterministic compile/test failures.
    /// </summary>
    bool IsRetryable(ExecutionResult execution);

    /// <summary>
    /// True for retryable .NET exceptions (timeouts, transient network errors, docker hiccups).
    /// </summary>
    bool IsRetryableException(Exception exception);

    /// <summary>
    /// True when the failure is environment-level (pip mirror down, DNS failure, certificate
    /// expired, sandbox FS denied) and code patches cannot fix it. Run should abort early.
    /// </summary>
    bool IsNonActionableInfrastructure(IReadOnlyList<ErrorReport> errors, ExecutionResult execution);
}

/// <summary>
/// Default classifier built from disjoint regex sets so each rule is auditable and
/// self-documenting. Any new rule should land here with a covering unit test.
/// </summary>
public sealed class DefaultExecutionFailureClassifier : IExecutionFailureClassifier
{
    private static readonly System.Text.RegularExpressions.Regex RetryableLogPattern =
        new(@"\b(timed?\s*out|temporarily\s+unavailable|connection\s+reset|connection\s+refused|EAI_AGAIN|ECONNRESET|TLS\s+handshake\s+failed|503\s+service|429\s+too\s+many|i/o\s+timeout)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

    private static readonly System.Text.RegularExpressions.Regex DockerInfraPattern =
        new(@"\bdocker[^\n]{0,40}(failed|error|cannot\s+connect|daemon\s+not\s+running)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

    private static readonly System.Text.RegularExpressions.Regex PipFailurePattern =
        new(@"(could\s+not\s+find\s+a\s+version\s+that\s+satisfies|ERROR:\s*pip|ERROR:\s*Could\s+not\s+install|No\s+matching\s+distribution\s+found)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

    private static readonly System.Text.RegularExpressions.Regex DnsTlsPattern =
        new(@"(failed\s+to\s+establish\s+a\s+new\s+connection|temporary\s+failure\s+in\s+name\s+resolution|no\s+such\s+host\s+is\s+known|certificate\s+verify\s+failed|SSL_ERROR_)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

    private static readonly System.Text.RegularExpressions.Regex SandboxFsPattern =
        new(@"(permission\s+denied[^\n]{0,80}/(tmp|sandbox|workspace)|read-only\s+file\s+system)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(200));

    public bool IsRetryable(ExecutionResult execution)
    {
        if (execution is null) return false;
        // Look only at recent log tail to avoid being fooled by application-level noise upstream.
        var tail = string.Join('\n', execution.Logs.TakeLast(40).Select(x => x.Message));
        if (RetryableLogPattern.IsMatch(tail)) return true;
        if (DockerInfraPattern.IsMatch(tail)) return true;
        return false;
    }

    public bool IsRetryableException(Exception exception)
    {
        if (exception is null) return false;
        if (exception is TimeoutException) return true;
        if (exception is TaskCanceledException) return true;
        var msg = exception.Message ?? string.Empty;
        if (RetryableLogPattern.IsMatch(msg)) return true;
        if (DockerInfraPattern.IsMatch(msg)) return true;
        return false;
    }

    public bool IsNonActionableInfrastructure(IReadOnlyList<ErrorReport> errors, ExecutionResult execution)
    {
        if (errors is null) errors = Array.Empty<ErrorReport>();
        var errorBlob = string.Join('\n', errors.Select(e => $"{e.ErrorType} {e.Message} {e.SuggestedFix}"));
        var execBlob = execution is null
            ? string.Empty
            : string.Join('\n', execution.ErrorLogs.TakeLast(80).Select(l => l.Message));
        var combined = errorBlob + "\n" + execBlob;
        if (string.IsNullOrWhiteSpace(combined)) return false;

        if (PipFailurePattern.IsMatch(combined)) return true;
        if (DnsTlsPattern.IsMatch(combined)) return true;
        if (SandboxFsPattern.IsMatch(combined)) return true;

        // Only treat docker-network breakage as infra; isolated docker reference inside
        // application stack traces shouldn't terminate the run.
        if (combined.Contains("docker", StringComparison.OrdinalIgnoreCase)
            && combined.Contains("network", StringComparison.OrdinalIgnoreCase)
            && DockerInfraPattern.IsMatch(combined))
            return true;

        return false;
    }
}
