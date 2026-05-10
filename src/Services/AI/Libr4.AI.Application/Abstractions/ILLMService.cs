using System.Threading.Tasks;

namespace Libr4.AI.Application.Abstractions;

public interface ILLMService
{
    Task<string> GenerateCodeAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> ExplainCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<float[]> GetEmbeddingsAsync(string text, CancellationToken cancellationToken = default);
}