namespace Libr4.AI.Api.Models;

public sealed record ChatMessage(string Role, string Content);

public sealed record ChatCompletionRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    double? Temperature = null,
    int? MaxTokens = null,
    bool Stream = false,
    string? Provider = null);

public sealed record ChatCompletionResponse(
    string Id,
    string Model,
    string Provider,
    IReadOnlyList<ChatMessage> Choices,
    int? PromptTokens,
    int? CompletionTokens);
