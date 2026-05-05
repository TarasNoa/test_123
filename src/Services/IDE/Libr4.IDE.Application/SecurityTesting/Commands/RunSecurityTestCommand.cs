using MediatR;
using Libr4.IDE.Application.SecurityTesting.DTOs;

namespace Libr4.IDE.Application.SecurityTesting.Commands;

/// <summary>
/// Command to run security test
/// </summary>
public record RunSecurityTestCommand : IRequest<SecurityTestResultDto>
{
    public string WorkspaceId { get; init; } = string.Empty;
    public string TestType { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
}
