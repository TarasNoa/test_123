namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// Overall status of an autonomous app generation orchestration
/// </summary>
public enum GenerationStatus
{
    /// <summary>Orchestrator is analyzing the request and building a plan</summary>
    Planning = 0,
    /// <summary>Agents are generating application code</summary>
    Generating = 1,
    /// <summary>Generated code is being executed and tested in shadow workspace</summary>
    Testing = 2,
    /// <summary>Errors detected; fixer agents are analyzing logs and applying fixes</summary>
    Fixing = 3,
    /// <summary>Application runs without errors and passes all tests</summary>
    Completed = 4,
    /// <summary>Max iterations reached or unrecoverable failure</summary>
    Failed = 5
}
