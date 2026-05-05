using Libr4.Shared.Contracts.Streaming;

namespace Libr4.Shared.Contracts.Sandbox;

/// <summary>
/// Code fragment schema
/// </summary>
public class CodeFragmentSchema
{
    public string Language { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool HasAdditionalDependencies { get; set; }
    public string? InstallDependenciesCommand { get; set; }
    public string? FilePath { get; set; }
    public string? Template { get; set; }
}

/// <summary>
/// Sandbox template registry interface
/// </summary>
public interface ISandboxTemplateRegistry
{
    Task<SandboxTemplate?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SandboxTemplate>> ListTemplatesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Sandbox execution result.
/// </summary>
public record SandboxExecutionResult
{
    /// <summary>
    /// Sandbox ID.
    /// </summary>
    public string SandboxId { get; init; } = string.Empty;

    /// <summary>
    /// Template used.
    /// </summary>
    public string Template { get; init; } = string.Empty;

    /// <summary>
    /// Execution URL (for web apps).
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// Standard output.
    /// </summary>
    public string? Stdout { get; init; }

    /// <summary>
    /// Standard error.
    /// </summary>
    public string? Stderr { get; init; }

    /// <summary>
    /// Runtime error (if any).
    /// </summary>
    public string? RuntimeError { get; init; }

    /// <summary>
    /// Cell results (for notebook-style execution).
    /// </summary>
    public List<object>? CellResults { get; init; }

    /// <summary>
    /// Whether execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Sandbox configuration.
/// </summary>
public record SandboxConfig
{
    /// <summary>
    /// E2B API key.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Sandbox timeout in milliseconds.
    /// </summary>
    public long TimeoutMs { get; init; } = 10 * 60 * 1000; // 10 minutes

    /// <summary>
    /// Whether to use team-based access.
    /// </summary>
    public bool UseTeamAccess { get; init; }

    /// <summary>
    /// Team ID (if using team access).
    /// </summary>
    public string? TeamId { get; init; }

    /// <summary>
    /// Access token (if using team access).
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// User ID for tracking.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Sandbox service interface.
/// </summary>
public interface ISandboxService
{
    /// <summary>
    /// Creates a sandbox from a template.
    /// </summary>
    /// <param name="templateId">Template ID.</param>
    /// <param name="config">Sandbox configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sandbox ID.</returns>
    Task<string> CreateSandboxAsync(
        string templateId,
        SandboxConfig config,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes code in a sandbox.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="code">Code to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result.</returns>
    Task<SandboxExecutionResult> ExecuteCodeAsync(
        string sandboxId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a file to the sandbox.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="filePath">File path.</param>
    /// <param name="content">File content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteFileAsync(
        string sandboxId,
        string filePath,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file from the sandbox.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="filePath">File path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File content.</returns>
    Task<string> ReadFileAsync(
        string sandboxId,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a command in the sandbox.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="command">Command to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    Task<SandboxExecutionResult> RunCommandAsync(
        string sandboxId,
        string command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the sandbox URL.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="port">Port number.</param>
    /// <returns>Sandbox URL.</returns>
    string GetSandboxUrl(string sandboxId, int port = 80);

    /// <summary>
    /// Kills a sandbox.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task KillSandboxAsync(
        string sandboxId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sandbox status.
    /// </summary>
    /// <param name="sandboxId">Sandbox ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sandbox status.</returns>
    Task<SandboxStatus> GetSandboxStatusAsync(
        string sandboxId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sandbox status.
/// </summary>
public record SandboxStatus
{
    /// <summary>
    /// Sandbox ID.
    /// </summary>
    public string SandboxId { get; init; } = string.Empty;

    /// <summary>
    /// Whether the sandbox is running.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    public float CpuUsage { get; init; }

    /// <summary>
    /// Memory usage in MB.
    /// </summary>
    public float MemoryUsage { get; init; }

    /// <summary>
    /// Uptime in seconds.
    /// </summary>
    public long UptimeSeconds { get; init; }
}

/// <summary>
/// In-memory sandbox service for development and testing.
/// </summary>
public class InMemorySandboxService : ISandboxService
{
    private readonly Dictionary<string, SandboxInstance> _sandboxes = new();
    private readonly object _lock = new();

    public async Task<string> CreateSandboxAsync(
        string templateId,
        SandboxConfig config,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var sandboxId = Guid.NewGuid().ToString();
        var instance = new SandboxInstance
        {
            SandboxId = sandboxId,
            TemplateId = templateId,
            Config = config,
            CreatedAt = DateTime.UtcNow,
            IsRunning = true,
            Files = new Dictionary<string, string>()
        };

        lock (_lock)
        {
            _sandboxes[sandboxId] = instance;
        }

        Console.WriteLine($"[Sandbox] Created sandbox {sandboxId} with template {templateId}");

        return sandboxId;
    }

    public async Task<SandboxExecutionResult> ExecuteCodeAsync(
        string sandboxId,
        string code,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        SandboxInstance? instance;
        lock (_lock)
        {
            _sandboxes.TryGetValue(sandboxId, out instance);
        }

        if (instance == null || !instance.IsRunning)
        {
            return new SandboxExecutionResult
            {
                SandboxId = sandboxId,
                Success = false,
                RuntimeError = "Sandbox not found or not running"
            };
        }

        // Mock execution - in production, this would call E2B SDK
        var startTime = DateTime.UtcNow;

        try
        {
            // Simulate execution
            await Task.Delay(100, cancellationToken);

            var result = new SandboxExecutionResult
            {
                SandboxId = sandboxId,
                Template = instance.TemplateId,
                Success = true,
                Stdout = "Code executed successfully",
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };

            return result;
        }
        catch (Exception ex)
        {
            return new SandboxExecutionResult
            {
                SandboxId = sandboxId,
                Template = instance.TemplateId,
                Success = false,
                RuntimeError = ex.Message,
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    public async Task WriteFileAsync(
        string sandboxId,
        string filePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (_sandboxes.TryGetValue(sandboxId, out var instance))
            {
                instance.Files[filePath] = content;
                Console.WriteLine($"[Sandbox] Wrote file {filePath} to sandbox {sandboxId}");
            }
        }
    }

    public async Task<string> ReadFileAsync(
        string sandboxId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (_sandboxes.TryGetValue(sandboxId, out var instance))
            {
                if (instance.Files.TryGetValue(filePath, out var content))
                {
                    return content;
                }
            }
        }

        throw new FileNotFoundException($"File not found: {filePath}");
    }

    public async Task<SandboxExecutionResult> RunCommandAsync(
        string sandboxId,
        string command,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        SandboxInstance? instance;
        lock (_lock)
        {
            _sandboxes.TryGetValue(sandboxId, out instance);
        }

        if (instance == null || !instance.IsRunning)
        {
            return new SandboxExecutionResult
            {
                SandboxId = sandboxId,
                Success = false,
                RuntimeError = "Sandbox not found or not running"
            };
        }

        // Mock command execution
        await Task.Delay(100, cancellationToken);

        return new SandboxExecutionResult
        {
            SandboxId = sandboxId,
            Template = instance.TemplateId,
            Success = true,
            Stdout = $"Command executed: {command}",
            DurationMs = 100
        };
    }

    public string GetSandboxUrl(string sandboxId, int port = 80)
    {
        // Mock URL - in production, this would return actual E2B URL
        return $"https://{sandboxId}.sandbox.e2b.dev:{port}";
    }

    public async Task KillSandboxAsync(
        string sandboxId,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (_sandboxes.TryGetValue(sandboxId, out var instance))
            {
                instance.IsRunning = false;
                Console.WriteLine($"[Sandbox] Killed sandbox {sandboxId}");
            }
        }
    }

    public async Task<SandboxStatus> GetSandboxStatusAsync(
        string sandboxId,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        lock (_lock)
        {
            if (_sandboxes.TryGetValue(sandboxId, out var instance))
            {
                return new SandboxStatus
                {
                    SandboxId = sandboxId,
                    IsRunning = instance.IsRunning,
                    CpuUsage = 0.0f,
                    MemoryUsage = 0.0f,
                    UptimeSeconds = (long)(DateTime.UtcNow - instance.CreatedAt).TotalSeconds
                };
            }
        }

        return new SandboxStatus
        {
            SandboxId = sandboxId,
            IsRunning = false,
            CpuUsage = 0.0f,
            MemoryUsage = 0.0f,
            UptimeSeconds = 0
        };
    }
}

/// <summary>
/// Internal sandbox instance.
/// </summary>
internal class SandboxInstance
{
    public string SandboxId { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public SandboxConfig Config { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsRunning { get; set; }
    public Dictionary<string, string> Files { get; set; } = new();
}

/// <summary>
/// Sandbox execution service for code fragments.
/// </summary>
public class SandboxExecutionService
{
    private readonly ISandboxService _sandboxService;
    private readonly ISandboxTemplateRegistry _templateRegistry;

    public SandboxExecutionService(
        ISandboxService sandboxService,
        ISandboxTemplateRegistry templateRegistry)
    {
        _sandboxService = sandboxService;
        _templateRegistry = templateRegistry;
    }

    /// <summary>
    /// Executes a code fragment in a sandbox.
    /// </summary>
    public async Task<SandboxExecutionResult> ExecuteFragmentAsync(
        CodeFragmentSchema fragment,
        SandboxConfig config,
        CancellationToken cancellationToken = default)
    {
        var template = await _templateRegistry.GetTemplateAsync(fragment.Template, cancellationToken);
        if (template == null)
        {
            return new SandboxExecutionResult
            {
                Template = fragment.Template,
                Success = false,
                RuntimeError = $"Template not found: {fragment.Template}"
            };
        }

        // Create sandbox
        var sandboxId = await _sandboxService.CreateSandboxAsync(fragment.Template, config, cancellationToken);

        try
        {
            // Install additional dependencies
            if (fragment.HasAdditionalDependencies && !string.IsNullOrEmpty(fragment.InstallDependenciesCommand))
            {
                await _sandboxService.RunCommandAsync(sandboxId, fragment.InstallDependenciesCommand, cancellationToken);
            }

            // Write code to file
            await _sandboxService.WriteFileAsync(sandboxId, fragment.FilePath, fragment.Code, cancellationToken);

            // Execute or return URL
            if (template.Port == null)
            {
                // Execute code (e.g., Python interpreter)
                return await _sandboxService.ExecuteCodeAsync(sandboxId, fragment.Code, cancellationToken);
            }
            else
            {
                // Return URL for web app
                var url = _sandboxService.GetSandboxUrl(sandboxId, template.Port);
                return new SandboxExecutionResult
                {
                    SandboxId = sandboxId,
                    Template = fragment.Template,
                    Url = url,
                    Success = true
                };
            }
        }
        catch (Exception ex)
        {
            return new SandboxExecutionResult
            {
                SandboxId = sandboxId,
                Template = fragment.Template,
                Success = false,
                RuntimeError = ex.Message
            };
        }
        finally
        {
            // Cleanup sandbox (optional - could keep it alive for debugging)
            await _sandboxService.KillSandboxAsync(sandboxId, cancellationToken);
        }
    }
}
