namespace Libr4.IDE.Application.MultiAgentOrchestration;

using Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Interface for pipeline execution engine
/// </summary>
public interface IPipelineExecutionEngine
{
    /// <summary>
    /// Execute a pipeline with given orchestration and options
    /// </summary>
    Task<PipelineExecutionResult> ExecutePipelineAsync(
        AgentOrchestration orchestration,
        PipelineExecutionOptions options,
        CancellationToken ct = default);
    
    /// <summary>
    /// Pause a running pipeline
    /// </summary>
    Task PausePipelineAsync(string pipelineId, CancellationToken ct = default);
    
    /// <summary>
    /// Resume a paused pipeline
    /// </summary>
    Task ResumePipelineAsync(string pipelineId, CancellationToken ct = default);
    
    /// <summary>
    /// Cancel a running pipeline
    /// </summary>
    Task CancelPipelineAsync(string pipelineId, CancellationToken ct = default);
    
    /// <summary>
    /// Rollback a pipeline to a specific phase
    /// </summary>
    Task RollbackPipelineAsync(string pipelineId, string targetPhaseId, CancellationToken ct = default);
    
    /// <summary>
    /// Get pipeline execution status
    /// </summary>
    Task<PipelineExecutionResult?> GetPipelineStatusAsync(string pipelineId, CancellationToken ct = default);
}
