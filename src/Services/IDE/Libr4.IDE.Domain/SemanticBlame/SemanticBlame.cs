using Libr4.IDE.Domain.Common;
using Libr4.IDE.Domain.SemanticBlame.Events;

namespace Libr4.IDE.Domain.SemanticBlame;

/// <summary>
/// AggregateRoot for semantic blame
/// </summary>
public class SemanticBlame : AggregateRoot<Guid>
{
    public string BlameId { get; private set; }
    public string FilePath { get; private set; }
    public List<BlameEntry> Entries { get; private set; }
    public CodeEvolution? Evolution { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    private SemanticBlame() { }
    
    public SemanticBlame(
        string blameId,
        string filePath,
        List<BlameEntry>? entries = null,
        CodeEvolution? evolution = null)
    {
        Id = Guid.NewGuid();
        BlameId = blameId;
        FilePath = filePath;
        Entries = entries ?? new List<BlameEntry>();
        Evolution = evolution;
        Status = "initializing";
        CreatedAt = DateTime.UtcNow;
        CompletedAt = null;
    }
    
    public void AddEntry(BlameEntry entry)
    {
        if (entry != null)
        {
            Entries.Add(entry);
        }
    }
    
    public void SetEvolution(CodeEvolution evolution)
    {
        Evolution = evolution;
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
    /// Marks the blame as generated and raises a domain event
    /// </summary>
    public void MarkBlameGenerated()
    {
        AddDomainEvent(new BlameGeneratedEvent(Id, BlameId));
    }
    
    /// <summary>
    /// Marks the evolution as analyzed and raises a domain event
    /// </summary>
    public void MarkEvolutionAnalyzed()
    {
        AddDomainEvent(new EvolutionAnalyzedEvent(Id, BlameId));
    }
    
    /// <summary>
    /// Marks the blame as completed and raises a domain event
    /// </summary>
    public void MarkAsCompleted()
    {
        AddDomainEvent(new BlameCompletedEvent(Id, BlameId, Entries.Count));
    }
    
    public static SemanticBlame Create(
        string blameId,
        string filePath,
        List<BlameEntry>? entries = null,
        CodeEvolution? evolution = null)
    {
        return new SemanticBlame(blameId, filePath, entries, evolution);
    }
}
