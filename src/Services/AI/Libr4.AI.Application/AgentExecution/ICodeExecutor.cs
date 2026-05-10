using System.Threading.Tasks;

namespace Libr4.AI.Application.AgentExecution;

// Stub interface to avoid circular dependency with Libr4.IDE.Domain
public interface ICodeExecutor
{
    Task<object> ExecuteAsync(string code, string language, int timeoutSeconds = 30);
}
