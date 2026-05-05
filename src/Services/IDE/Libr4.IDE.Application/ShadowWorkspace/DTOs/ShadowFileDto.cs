namespace Libr4.IDE.Application.ShadowWorkspace.DTOs;

/// <summary>
/// DTO for ShadowFile
/// </summary>
public record ShadowFileDto
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<ValidationResultDto> ValidationResults { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}
