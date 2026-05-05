namespace Libr4.IDE.Application.HackerAgent.DTOs;

/// <summary>
/// DTO for SecurityScript
/// </summary>
public record SecurityScriptDto
{
    public Guid Id { get; init; }
    public string ScriptName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string ScriptContent { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
}
