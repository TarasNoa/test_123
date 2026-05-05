namespace Libr4.IDE.Application.SeniorRolePrompts.DTOs;

/// <summary>
/// DTO for SeniorRole
/// </summary>
public record SeniorRoleDto
{
    public string RoleTitle { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Responsibilities { get; init; } = new();
    public List<string> Capabilities { get; init; } = new();
    public string ReviewProfile { get; init; } = string.Empty;
}
