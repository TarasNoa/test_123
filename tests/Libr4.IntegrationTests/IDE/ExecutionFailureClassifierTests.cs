using FluentAssertions;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;
using Libr4.IDE.Domain.AutonomousAppGeneration;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class ExecutionFailureClassifierTests
{
    private readonly DefaultExecutionFailureClassifier _sut = new();

    [Fact]
    public void IsRetryable_TimeoutInLogs_ReturnsTrue()
    {
        var execution = MakeExecution(
            stdout: new[] { "starting build", "test step timed out after 30s" },
            stderr: Array.Empty<string>());

        _sut.IsRetryable(execution).Should().BeTrue();
    }

    [Fact]
    public void IsRetryable_ConnectionReset_ReturnsTrue()
    {
        var execution = MakeExecution(
            stdout: new[] { "ERROR: connection reset by peer" },
            stderr: Array.Empty<string>());

        _sut.IsRetryable(execution).Should().BeTrue();
    }

    [Fact]
    public void IsRetryable_DeterministicCompileError_ReturnsFalse()
    {
        var execution = MakeExecution(
            stdout: new[] { "Compilation failed: CS0246 missing namespace" },
            stderr: Array.Empty<string>());

        _sut.IsRetryable(execution).Should().BeFalse();
    }

    [Fact]
    public void IsRetryableException_TimeoutException_ReturnsTrue()
    {
        _sut.IsRetryableException(new TimeoutException()).Should().BeTrue();
        _sut.IsRetryableException(new TaskCanceledException()).Should().BeTrue();
    }

    [Fact]
    public void IsRetryableException_GenericArgumentException_ReturnsFalse()
    {
        _sut.IsRetryableException(new ArgumentException("bad arg")).Should().BeFalse();
    }

    [Fact]
    public void IsNonActionableInfrastructure_PipFailure_ReturnsTrue()
    {
        var errors = new[]
        {
            new ErrorReport(
                errorType: "BuildOrRuntimeError",
                message: "pip install failed",
                suggestedFix: string.Empty,
                filePath: null)
        };
        var execution = MakeExecution(
            stdout: Array.Empty<string>(),
            stderr: new[] { "ERROR: Could not find a version that satisfies the requirement strangepkg" });

        _sut.IsNonActionableInfrastructure(errors, execution).Should().BeTrue();
    }

    [Fact]
    public void IsNonActionableInfrastructure_DnsFailure_ReturnsTrue()
    {
        var execution = MakeExecution(
            stdout: Array.Empty<string>(),
            stderr: new[] { "Failed to establish a new connection: temporary failure in name resolution" });

        _sut.IsNonActionableInfrastructure(Array.Empty<ErrorReport>(), execution).Should().BeTrue();
    }

    [Fact]
    public void IsNonActionableInfrastructure_ApplicationCompileError_ReturnsFalse()
    {
        var errors = new[]
        {
            new ErrorReport("CompileError", "CS0103: name not in context", string.Empty, "src/Foo.cs")
        };
        var execution = MakeExecution(
            stdout: new[] { "Building..." },
            stderr: new[] { "src/Foo.cs(12,5): error CS0103" });

        _sut.IsNonActionableInfrastructure(errors, execution).Should().BeFalse();
    }

    private static ExecutionResult MakeExecution(IReadOnlyList<string> stdout, IReadOnlyList<string> stderr)
    {
        var logs = stdout.Select(m => new ConsoleLogEntry(DateTime.UtcNow, "stdout", m)).ToList();
        var errLogs = stderr.Select(m => new ConsoleLogEntry(DateTime.UtcNow, "stderr", m)).ToList();
        var allLogs = logs.Concat(errLogs).ToList();
        var succeeded = stderr.Count == 0;
        return new ExecutionResult(
            succeeded: succeeded,
            exitCode: succeeded ? 0 : 1,
            duration: TimeSpan.FromMilliseconds(1),
            logs: allLogs,
            commandExecutions: null,
            testReport: null);
    }
}
