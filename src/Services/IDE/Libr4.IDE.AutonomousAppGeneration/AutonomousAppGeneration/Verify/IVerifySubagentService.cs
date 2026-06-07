using Libr4.IDE.Application.AutonomousAppGeneration.Services.Pipeline;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public interface IVerifySubagentService
{
    Task<VerifySubagentResult> RunAsync(GenerationContext context, CancellationToken ct = default);
}
