namespace Libr4.IDE.Application.AutonomousAppGeneration.InlineCompletion;

public interface IInlineCompletionService
{
    Task<InlineCompletionResult> CompleteAsync(InlineCompletionRequest request, CancellationToken ct = default);
}
