using System.Threading.Tasks;

namespace Libr4.AI.Application.AgentExecution;

// Stub interface to avoid circular dependency with Libr4.IDE.Domain
public interface ICodeGenerationService
{
    Task<string> GenerateCodeAsync(string prompt, string context, string language);
}
