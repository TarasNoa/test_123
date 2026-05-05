namespace Libr4.IDE.Application.CodeIntelligence.DTOs;

/// <summary>
/// DTO for CompletionContext
/// </summary>
public record CompletionContextDto
{
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }
    public string Prefix { get; init; } = string.Empty;
    public string SurroundingCode { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
}
