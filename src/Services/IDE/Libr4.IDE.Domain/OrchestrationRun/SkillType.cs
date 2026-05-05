namespace Libr4.IDE.Domain.OrchestrationRun;

/// <summary>
/// Represents the type of skill for orchestration
/// </summary>
public enum SkillType
{
    /// <summary>
    /// Analyze and plan requests
    /// </summary>
    PlanRequest = 1,
    
    /// <summary>
    /// Edit multiple files coherently
    /// </summary>
    MultiFileEdit = 2,
    
    /// <summary>
    /// Run validation loops with feedback
    /// </summary>
    ValidationLoop = 3,
    
    /// <summary>
    /// QA automation
    /// </summary>
    QAAutomation = 4,
    
    /// <summary>
    /// Code review with guardrails
    /// </summary>
    CodeReview = 5,
    
    /// <summary>
    /// Security review
    /// </summary>
    SecurityReview = 6,
    
    /// <summary>
    /// Testing
    /// </summary>
    Testing = 7,
    
    /// <summary>
    /// Documentation
    /// </summary>
    Documentation = 8
}
