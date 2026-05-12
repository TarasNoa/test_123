using System.Threading.Tasks;

namespace Libr4.AI.Application.AgentExecution;

public interface ICodeRepairService
{
    Task<string?> RepairCodeAsync(string code, ErrorAnalysis errorAnalysis, string language);
    Task<(bool Success, string RepairedCode)> AttemptAutoFixAsync(string code, string errorMessage, string language);
}
