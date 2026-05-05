using MediatR;
using Libr4.IDE.Domain.GitHubBootstrap;
using Libr4.IDE.Application.GitHubBootstrap.DTOs;

namespace Libr4.IDE.Application.GitHubBootstrap.Commands;

/// <summary>
/// Command to bootstrap a project
/// </summary>
public record BootstrapProjectCommand : IRequest<BootstrapProjectDto>
{
    public string ProjectName { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public List<LicenseType> AllowedLicenses { get; init; } = new();
}
