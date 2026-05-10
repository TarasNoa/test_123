using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Libr4.IDE.Domain.AgentExecution;

public interface ICodeExecutor
{
    Task<ExecutionResult> ExecuteAsync(string code, string language, int timeoutSeconds = 30);
    Task<ExecutionResult> ExecuteCSharpAsync(string code, int timeoutSeconds = 30);
    Task<ExecutionResult> ExecuteFSharpAsync(string code, int timeoutSeconds = 30);
    Task<ExecutionResult> ExecuteTypeScriptAsync(string code, int timeoutSeconds = 30);
}

public class CodeExecutor : ICodeExecutor
{
    private readonly ILogger<CodeExecutor> _logger;

    public CodeExecutor(ILogger<CodeExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<ExecutionResult> ExecuteAsync(string code, string language, int timeoutSeconds = 30)
    {
        return language.ToLower() switch
        {
            "csharp" or "cs" => await ExecuteCSharpAsync(code, timeoutSeconds),
            "fsharp" or "fs" => await ExecuteFSharpAsync(code, timeoutSeconds),
            "typescript" or "ts" or "javascript" or "js" => await ExecuteTypeScriptAsync(code, timeoutSeconds),
            _ => throw new NotSupportedException($"Language {language} is not supported")
        };
    }

    public async Task<ExecutionResult> ExecuteCSharpAsync(string code, int timeoutSeconds = 30)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Create a temporary file
            var tempFile = Path.Combine(Path.GetTempPath(), $"agent_{Guid.NewGuid()}.cs");
            await File.WriteAllTextAsync(tempFile, WrapCSharpCode(code));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"script run \"{tempFile}\" --no-cache",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();

            var completed = process.WaitForExit(timeoutSeconds * 1000);
            stopwatch.Stop();

            if (!completed)
            {
                process.Kill();
                return new ExecutionResult
                {
                    Status = ExecutionStatus.Failed,
                    ErrorMessage = "Execution timeout exceeded",
                    ExecutionTime = stopwatch.Elapsed,
                    AttemptNumber = 1
                };
            }

            File.Delete(tempFile);

            if (process.ExitCode != 0)
            {
                return new ExecutionResult
                {
                    Status = ExecutionStatus.FixRequired,
                    ErrorMessage = await error,
                    Output = await output,
                    ExecutionTime = stopwatch.Elapsed,
                    AttemptNumber = 1
                };
            }

            return new ExecutionResult
            {
                Status = ExecutionStatus.Success,
                Output = await output,
                ExecutionTime = stopwatch.Elapsed,
                AttemptNumber = 1
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError($"Error executing C# code: {ex.Message}");
            return new ExecutionResult
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                ExecutionTime = stopwatch.Elapsed,
                AttemptNumber = 1
            };
        }
    }

    public async Task<ExecutionResult> ExecuteFSharpAsync(string code, int timeoutSeconds = 30)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"agent_{Guid.NewGuid()}.fsx");
            await File.WriteAllTextAsync(tempFile, code);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"fsi \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();

            var completed = process.WaitForExit(timeoutSeconds * 1000);
            stopwatch.Stop();

            if (!completed)
            {
                process.Kill();
                return new ExecutionResult
                {
                    Status = ExecutionStatus.Failed,
                    ErrorMessage = "Execution timeout exceeded",
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            File.Delete(tempFile);

            if (process.ExitCode != 0)
            {
                return new ExecutionResult
                {
                    Status = ExecutionStatus.FixRequired,
                    ErrorMessage = await error,
                    Output = await output,
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            return new ExecutionResult
            {
                Status = ExecutionStatus.Success,
                Output = await output,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ExecutionResult
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<ExecutionResult> ExecuteTypeScriptAsync(string code, int timeoutSeconds = 30)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"agent_{Guid.NewGuid()}.ts");
            await File.WriteAllTextAsync(tempFile, code);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = $"--loader ts-node/esm \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();

            var completed = process.WaitForExit(timeoutSeconds * 1000);
            stopwatch.Stop();

            if (!completed)
            {
                process.Kill();
                return new ExecutionResult
                {
                    Status = ExecutionStatus.Failed,
                    ErrorMessage = "Execution timeout exceeded",
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            File.Delete(tempFile);

            if (process.ExitCode != 0)
            {
                return new ExecutionResult
                {
                    Status = ExecutionStatus.FixRequired,
                    ErrorMessage = await error,
                    Output = await output,
                    ExecutionTime = stopwatch.Elapsed
                };
            }

            return new ExecutionResult
            {
                Status = ExecutionStatus.Success,
                Output = await output,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ExecutionResult
            {
                Status = ExecutionStatus.Failed,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    private static string WrapCSharpCode(string code)
    {
        return $@"
using System;
using System.Collections.Generic;
using System.Linq;

{code}
";
    }
}