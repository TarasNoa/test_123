using Libr4.AI.Domain.Chats;

namespace Libr4.AI.Application.Abstractions;

public interface ILLMProviderFactory
{
    ILLMProvider GetProvider(AIProviderType type);
    ILLMProvider GetProvider(string model);
    IEnumerable<string> GetAvailableModels();
}
