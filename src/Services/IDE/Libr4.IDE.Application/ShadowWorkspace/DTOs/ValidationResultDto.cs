namespace Libr4.IDE.Application.ShadowWorkspace.DTOs;

/// <summary>
/// DTO for ValidationResult
/// </summary>
public record ValidationResultDto
{
    public string Type { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public double DurationMs { get; init; }
}
