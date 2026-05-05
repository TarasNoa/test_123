namespace Libr4.IDE.Application.ShadowWorkspace;

/// <summary>
/// Interface for self-healing build pipeline
/// </summary>
public interface ISelfHealingBuildPipeline
{
    Task<BuildResult> BuildAsync(string projectPath, BuildOptions? options = null, CancellationToken ct = default);
    Task<bool> DiagnoseAndFixAsync(string projectPath, BuildError error, CancellationToken ct = default);
}

public class BuildOptions
{
    public string Configuration { get; set; } = "Release";
    public bool SelfHeal { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
}

public class BuildResult
{
    public bool Success { get; set; }
    public string[] Output { get; set; } = Array.Empty<string>();
    public BuildError[] Errors { get; set; } = Array.Empty<BuildError>();
    public TimeSpan Duration { get; set; }
    public int RetryCount { get; set; }
}

public class BuildError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public int? LineNumber { get; set; }
}
