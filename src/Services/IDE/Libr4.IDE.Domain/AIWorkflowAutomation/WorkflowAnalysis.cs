using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.AIWorkflowAutomation.Events;

namespace Libr4.IDE.Domain.AIWorkflowAutomation;

/// <summary>
/// AggregateRoot for workflow analysis
/// </summary>
public class WorkflowAnalysis : AggregateRoot<Guid>
{
    public string AnalysisId { get; private set; }
    public string WorkflowId { get; private set; }
    public List<WorkflowPattern> Patterns { get; private set; }
    public List<ExtractedSkill> ExtractedSkills { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private WorkflowAnalysis() { }
    
    public WorkflowAnalysis(
        string analysisId,
        string workflowId,
        List<WorkflowPattern>? patterns = null,
        List<ExtractedSkill>? extractedSkills = null)
    {
        Id = Guid.NewGuid();
        AnalysisId = analysisId;
        WorkflowId = workflowId;
        Patterns = patterns ?? new List<WorkflowPattern>();
        ExtractedSkills = extractedSkills ?? new List<ExtractedSkill>();
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddPattern(WorkflowPattern pattern)
    {
        if (pattern != null)
        {
            Patterns.Add(pattern);
        }
    }
    
    public void AddExtractedSkill(ExtractedSkill skill)
    {
        if (skill != null)
        {
            ExtractedSkills.Add(skill);
        }
    }
    
    public void SetStatus(string status)
    {
        Status = status;
        if (status == "completed" || status == "failed")
        {
            CompletedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Marks the analysis as started and raises a domain event
    /// </summary>
    public void MarkAsStarted()
    {
        AddDomainEvent(new AnalysisStartedEvent(Id, AnalysisId));
    }
    
    /// <summary>
    /// Marks a skill as extracted and raises a domain event
    /// </summary>
    public void MarkSkillExtracted(ExtractedSkill skill)
    {
        AddDomainEvent(new SkillExtractedEvent(Id, AnalysisId, skill.SkillName));
    }
    
    /// <summary>
    /// Marks a pattern as detected and raises a domain event
    /// </summary>
    public void MarkPatternDetected(WorkflowPattern pattern)
    {
        AddDomainEvent(new PatternDetectedEvent(Id, AnalysisId, pattern.PatternName));
    }
    
    public static WorkflowAnalysis Create(
        string analysisId,
        string workflowId,
        List<WorkflowPattern>? patterns = null,
        List<ExtractedSkill>? extractedSkills = null)
    {
        return new WorkflowAnalysis(analysisId, workflowId, patterns, extractedSkills);
    }
}
