using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libr4.Chat.Application.Abstractions;
using Libr4.Chat.Domain.CodeSnippets;
using Microsoft.EntityFrameworkCore;

namespace Libr4.Chat.Application.CodeSnippets;

public class CodeSnippetService : ICodeSnippetService
{
    private readonly IChatDbContext _dbContext;

    public CodeSnippetService(IChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CodeSnippetDto> CreateSnippetAsync(CreateCodeSnippetRequest request, Guid creatorId)
    {
        var snippet = CodeSnippet.Create(Guid.NewGuid(), request.ChannelId, creatorId, request.Language, request.Code, request.Title);
        await _dbContext.CodeSnippets.AddAsync(snippet);
        await _dbContext.SaveChangesAsync();

        return MapToDto(snippet);
    }

    public async Task<CodeSnippetDto> GetSnippetAsync(Guid snippetId)
    {
        var snippet = await _dbContext.CodeSnippets.FindAsync(snippetId);
        if (snippet == null) throw new InvalidOperationException("Snippet not found");
        return MapToDto(snippet);
    }

    public async Task<List<CodeTemplateDto>> GetTemplatesAsync()
    {
        // Return a static list of common templates for now
        return new List<CodeTemplateDto>
        {
            new CodeTemplateDto(Guid.NewGuid(), "csharp", "HTTP Client", "using var client = new HttpClient();"),
            new CodeTemplateDto(Guid.NewGuid(), "typescript", "React Component", "export const Component = () => {}"),
            new CodeTemplateDto(Guid.NewGuid(), "python", "FastAPI Endpoint", "@app.get(\"/items\")"),
        };
    }

    public async Task<List<CodeSnippetDto>> GetChannelSnippetsAsync(Guid channelId)
    {
        var snippets = await _dbContext.CodeSnippets
            .Where(s => s.ChannelId == channelId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return snippets.Select(MapToDto).ToList();
    }

    private static CodeSnippetDto MapToDto(CodeSnippet s) => new(
        s.Id,
        s.ChannelId,
        s.CreatorId,
        s.Language,
        s.Code,
        s.Title,
        s.CreatedAt);
}
