using System.Threading.Tasks;

namespace Libr4.AI.Infrastructure.ML;

public interface IRustInferenceBridge
{
    Task<string> RunInferenceAsync(string requestJson);
}