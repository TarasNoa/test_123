using MediatR;
using Libr4.IDE.Application.CodeIntelligence.DTOs;

namespace Libr4.IDE.Application.CodeIntelligence.Commands;

/// <summary>
/// Command to get code completions
/// </summary>
public record GetCompletionsCommand : IRequest<CodeIntelligenceDto>
{
    public string FilePath { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }
    public string Code { get; init; } = string.Empty;
}
