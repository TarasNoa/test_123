using MediatR;

namespace Libr4.IDE.Application.AI.Commands;

public record GenerateUICommand(
    string WorkspaceId,
    string Prompt,
    bool UseTambo = true
) : IRequest<GenerateUIResult>;

public record GenerateUIResult(
    bool Success,
    string? Code = null,
    string? ComponentName = null,
    string? Error = null
);
