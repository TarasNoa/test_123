using System.Threading.Tasks;

namespace Libr4.AI.Application.ML;

// Stub interface to avoid circular dependency with Libr4.AI.Infrastructure
public interface IRustInferenceBridge
{
    Task<string> RunInferenceAsync(string requestJson);
}
