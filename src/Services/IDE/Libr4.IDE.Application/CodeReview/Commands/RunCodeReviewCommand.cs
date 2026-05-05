using MediatR;
using Libr4.IDE.Domain.CodeReview;
using Libr4.IDE.Application.CodeReview.DTOs;

namespace Libr4.IDE.Application.CodeReview.Commands;

/// <summary>
/// Command to run a code review
/// </summary>
public record RunCodeReviewCommand : IRequest<CodeReviewDto>
{
    public string WorkspaceId { get; init; } = string.Empty;
    public List<string> Files { get; init; } = new();
    public List<ReviewType> ReviewTypes { get; init; } = new();
}
