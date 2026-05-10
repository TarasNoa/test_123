using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libr4.Chat.Application.Abstractions;

public record CodeSnippetDto(
    Guid Id,
    Guid ChannelId,
    Guid CreatorId,
    string Language,
    string Code,
    string Title,
    DateTimeOffset CreatedAt);

public record CodeTemplateDto(
    Guid Id,
    string Language,
    string Name,
    string Code);

public interface ICodeSnippetService
{
    Task<CodeSnippetDto> CreateSnippetAsync(CreateCodeSnippetRequest request, Guid creatorId);
    Task<CodeSnippetDto> GetSnippetAsync(Guid snippetId);
    Task<List<CodeTemplateDto>> GetTemplatesAsync();
    Task<List<CodeSnippetDto>> GetChannelSnippetsAsync(Guid channelId);
}