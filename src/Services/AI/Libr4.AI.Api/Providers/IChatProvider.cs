using Libr4.AI.Api.Models;

namespace Libr4.AI.Api.Providers;

public interface IChatProvider
{
    string Name { get; }
    Task<ChatCompletionResponse> CompleteAsync(ChatCompletionRequest request, CancellationToken ct);
}
