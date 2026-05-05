using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.AI.Application.Abstractions;

public record ChatMessage(string Role, string Content);

public record ChatCompletionRequest(
    string Model,
    List<ChatMessage> Messages,
    float Temperature = 0.7f,
    int MaxTokens = 2000);

public record ChatCompletionResponse(
    string Content,
    int TokensUsed,
    string? FinishReason = null);

public interface ILLMProvider
{
    string Name { get; }
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<Result<ChatCompletionResponse>> CompleteAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamAsync(
        ChatCompletionRequest request,
        CancellationToken cancellationToken = default);
}
