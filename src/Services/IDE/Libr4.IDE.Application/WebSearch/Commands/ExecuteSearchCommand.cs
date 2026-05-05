using MediatR;
using Libr4.IDE.Domain.WebSearch;
using Libr4.IDE.Application.WebSearch.DTOs;

namespace Libr4.IDE.Application.WebSearch.Commands;

/// <summary>
/// Command to execute web search
/// </summary>
public record ExecuteSearchCommand : IRequest<WebSearchDto>
{
    public string Query { get; init; } = string.Empty;
    public List<SearchProvider> Providers { get; init; } = new();
}
