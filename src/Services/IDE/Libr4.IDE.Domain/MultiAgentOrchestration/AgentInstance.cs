namespace Libr4.IDE.Domain.MultiAgentOrchestration;

/// <summary>
/// Agent operation mode (from OpenAnalyst multi-mode operation)
/// </summary>
public enum AgentOperationMode
{
    /// <summary>
    /// Data Analyst mode - for planning data analysis
    /// </summary>
    DataAnalyst,
    
    /// <summary>
    /// Code mode - for writing, modifying, and refactoring code
    /// </summary>
    Code,
    
    /// <summary>
    /// Ask mode - for getting answers and explanations
    /// </summary>
    Ask,
    
    /// <summary>
    /// Debug mode - for diagnosing and fixing software issues
    /// </summary>
    Debug,
    
    /// <summary>
    /// Custom mode - user-defined specialized mode
    /// </summary>
    Custom
}

/// <summary>
/// Smart alert for task progress (from OpenAnalyst)
/// </summary>
public class SmartAlert
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsAcknowledged { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    
    public SmartAlert(string title, string message, AlertSeverity severity = AlertSeverity.Info)
    {
        Id = Guid.NewGuid();
        Title = title;
        Message = message;
        Severity = severity;
        CreatedAt = DateTime.UtcNow;
        IsAcknowledged = false;
    }
    
    public void Acknowledge()
    {
        IsAcknowledged = true;
        AcknowledgedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Alert severity level
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// Represents an instance of an AI agent in the orchestration
/// Enhanced with OpenAnalyst multi-mode operation and smart alerts
/// </summary>
public class AgentInstance
{
    public Guid Id { get; private set; }
    public string AgentType { get; private set; }
    public string Description { get; private set; }
    public AgentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActiveAt { get; private set; }
    
    /// <summary>
    /// Current operation mode (from OpenAnalyst)
    /// </summary>
    public AgentOperationMode CurrentMode { get; private set; }
    
    /// <summary>
    /// Available modes for this agent
    /// </summary>
    public List<AgentOperationMode> AvailableModes { get; private set; }
    
    /// <summary>
    /// Smart alerts for this agent (from OpenAnalyst)
    /// </summary>
    public List<SmartAlert> Alerts { get; private set; }
    
    /// <summary>
    /// Data analytics specialization flag (from OpenAnalyst)
    /// </summary>
    public bool HasDataAnalyticsSpecialization { get; private set; }
    
    /// <summary>
    /// Supported data libraries (from OpenAnalyst)
    /// </summary>
    public List<string> DataLibraries { get; private set; }
    
    public AgentInstance(string agentType, string description, AgentStatus status = AgentStatus.Idle)
    {
        Id = Guid.NewGuid();
        AgentType = agentType;
        Description = description;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        CurrentMode = AgentOperationMode.Code;
        AvailableModes = new List<AgentOperationMode> { AgentOperationMode.Code };
        Alerts = new List<SmartAlert>();
        HasDataAnalyticsSpecialization = false;
        DataLibraries = new List<string>();
        Checkpoints = new List<AgentCheckpoint>();
        ContextHistory = new List<ContextSnapshot>();
    }
    
    /// <summary>
    /// Switch operation mode (from OpenAnalyst)
    /// </summary>
    public void SwitchMode(AgentOperationMode mode)
    {
        if (AvailableModes.Contains(mode))
        {
            CurrentMode = mode;
            LastActiveAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Add available mode
    /// </summary>
    public void AddAvailableMode(AgentOperationMode mode)
    {
        if (!AvailableModes.Contains(mode))
        {
            AvailableModes.Add(mode);
        }
    }
    
    /// <summary>
    /// Add smart alert (from OpenAnalyst)
    /// </summary>
    public void AddAlert(SmartAlert alert)
    {
        Alerts.Add(alert);
    }
    
    /// <summary>
    /// Acknowledge alert
    /// </summary>
    public void AcknowledgeAlert(Guid alertId)
    {
        var alert = Alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            alert.Acknowledge();
        }
    }
    
    /// <summary>
    /// Enable data analytics specialization (from OpenAnalyst)
    /// </summary>
    public void EnableDataAnalyticsSpecialization(List<string>? libraries = null)
    {
        HasDataAnalyticsSpecialization = true;
        DataLibraries = libraries ?? new List<string> { "pandas", "numpy", "matplotlib", "scikit-learn" };
        AddAvailableMode(AgentOperationMode.DataAnalyst);
    }
    
    /// <summary>
    /// Get unacknowledged alerts
    /// </summary>
    public List<SmartAlert> GetUnacknowledgedAlerts()
    {
        return Alerts.Where(a => !a.IsAcknowledged).ToList();
    }
    
    /// <summary>
    /// Checkpoint for state navigation (from Roo-Code)
    /// </summary>
    public class AgentCheckpoint
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public Dictionary<string, object> State { get; private set; }
        public string Description { get; private set; }
        
        public AgentCheckpoint(string name, string description, Dictionary<string, object>? state = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            Description = description;
            State = state ?? new Dictionary<string, object>();
            CreatedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Checkpoints for state navigation (from Roo-Code)
    /// </summary>
    public List<AgentCheckpoint> Checkpoints { get; private set; }
    
    /// <summary>
    /// Context management (from Roo-Code)
    /// </summary>
    public class ContextSnapshot
    {
        public Guid Id { get; private set; }
        public DateTime CapturedAt { get; private set; }
        public List<string> ActiveFiles { get; private set; }
        public Dictionary<string, string> ContextData { get; private set; }
        public int TokenCount { get; private set; }
        
        public ContextSnapshot()
        {
            Id = Guid.NewGuid();
            CapturedAt = DateTime.UtcNow;
            ActiveFiles = new List<string>();
            ContextData = new Dictionary<string, string>();
            TokenCount = 0;
        }
    }
    
    /// <summary>
    /// Context snapshots (from Roo-Code)
    /// </summary>
    public List<ContextSnapshot> ContextHistory { get; private set; }
    
    /// <summary>
    /// Create checkpoint (from Roo-Code)
    /// </summary>
    public AgentCheckpoint CreateCheckpoint(string name, string description)
    {
        var checkpoint = new AgentCheckpoint(name, description);
        Checkpoints.Add(checkpoint);
        return checkpoint;
    }
    
    /// <summary>
    /// Restore checkpoint (from Roo-Code)
    /// </summary>
    public void RestoreCheckpoint(Guid checkpointId)
    {
        var checkpoint = Checkpoints.FirstOrDefault(c => c.Id == checkpointId);
        if (checkpoint != null)
        {
            // Restore state from checkpoint
            LastActiveAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Capture context snapshot (from Roo-Code)
    /// </summary>
    public ContextSnapshot CaptureContext()
    {
        var snapshot = new ContextSnapshot();
        ContextHistory.Add(snapshot);
        return snapshot;
    }
    
    public void SetStatus(AgentStatus status)
    {
        Status = status;
        LastActiveAt = DateTime.UtcNow;
    }
}
