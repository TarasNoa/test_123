namespace Libr4.Shared.Contracts.Artifacts;

/// <summary>
/// Base class for all artifacts generated during agent execution.
/// Artifacts are tangible deliverables that provide structured output
/// instead of raw tool calls.
/// </summary>
public abstract record Artifact
{
    /// <summary>
    /// Unique identifier for the artifact.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Type of artifact (e.g., "task_list", "implementation_plan", "screenshot").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable title for the artifact.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the artifact represents.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the artifact was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the agent or task that generated this artifact.
    /// </summary>
    public string? GeneratedBy { get; init; }

    /// <summary>
    /// Status of the artifact (draft, completed, approved, rejected).
    /// </summary>
    public ArtifactStatus Status { get; init; } = ArtifactStatus.Draft;

    /// <summary>
    /// Comments on the artifact.
    /// </summary>
    public List<ArtifactComment> Comments { get; init; } = new();
}

/// <summary>
/// Status of an artifact.
/// </summary>
public enum ArtifactStatus
{
    Draft,
    Completed,
    Approved,
    Rejected
}

/// <summary>
/// Comment on an artifact.
/// </summary>
public record ArtifactComment
{
    /// <summary>
    /// Unique identifier for the comment.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID of the user who made the comment.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Username of the commenter.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Comment text.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the comment was made.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the comment requires agent action.
    /// </summary>
    public bool RequiresAction { get; init; } = false;
}

/// <summary>
/// Task list artifact - structured list of tasks to be completed.
/// </summary>
public record TaskListArtifact : Artifact
{
    /// <summary>
    /// List of tasks in the task list.
    /// </summary>
    public List<TaskItem> Tasks { get; init; } = new();

    /// <summary>
    /// Total number of tasks.
    /// </summary>
    public int TotalTasks => Tasks.Count;

    /// <summary>
    /// Number of completed tasks.
    /// </summary>
    public int CompletedTasks => Tasks.Count(t => t.Status == TaskStatus.Completed);

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
}

/// <summary>
/// Individual task in a task list.
/// </summary>
public record TaskItem
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Task title.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Task description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Task status.
    /// </summary>
    public TaskStatus Status { get; init; } = TaskStatus.Pending;

    /// <summary>
    /// Task priority.
    /// </summary>
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    /// <summary>
    /// Estimated effort (in hours or story points).
    /// </summary>
    public int? EstimatedEffort { get; init; }

    /// <summary>
    /// Dependencies on other task IDs.
    /// </summary>
    public List<string> DependsOn { get; init; } = new();
}

/// <summary>
/// Status of a task.
/// </summary>
public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Blocked,
    Cancelled
}

/// <summary>
/// Priority of a task.
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Implementation plan artifact - structured plan for implementation.
/// </summary>
public record ImplementationPlanArtifact : Artifact
{
    /// <summary>
    /// Phases in the implementation plan.
    /// </summary>
    public List<ImplementationPhase> Phases { get; init; } = new();

    /// <summary>
    /// Tech stack to be used.
    /// </summary>
    public TechStackDefinition TechStack { get; init; } = new();

    /// <summary>
    /// Estimated timeline.
    /// </summary>
    public string? EstimatedTimeline { get; init; }

    /// <summary>
    /// Risks and mitigations.
    /// </summary>
    public List<RiskMitigation> Risks { get; init; } = new();
}

/// <summary>
/// Phase in an implementation plan.
/// </summary>
public record ImplementationPhase
{
    /// <summary>
    /// Phase number or identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Phase name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Phase description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Tasks in this phase.
    /// </summary>
    public List<string> TaskIds { get; init; } = new();
}

/// <summary>
/// Tech stack definition.
/// </summary>
public record TechStackDefinition
{
    /// <summary>
    /// Programming languages.
    /// </summary>
    public List<string> Languages { get; init; } = new();

    /// <summary>
    /// Frameworks and libraries.
    /// </summary>
    public List<string> Frameworks { get; init; } = new();

    /// <summary>
    /// Databases.
    /// </summary>
    public List<string> Databases { get; init; } = new();

    /// <summary>
    /// Infrastructure components.
    /// </summary>
    public List<string> Infrastructure { get; init; } = new();
}

/// <summary>
/// Risk and mitigation.
/// </summary>
public record RiskMitigation
{
    /// <summary>
    /// Risk description.
    /// </summary>
    public string Risk { get; init; } = string.Empty;

    /// <summary>
    /// Mitigation strategy.
    /// </summary>
    public string Mitigation { get; init; } = string.Empty;

    /// <summary>
    /// Risk severity.
    /// </summary>
    public RiskSeverity Severity { get; init; } = RiskSeverity.Medium;
}

/// <summary>
/// Risk severity level.
/// </summary>
public enum RiskSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Screenshot artifact - captured screenshot from browser or application.
/// </summary>
public record ScreenshotArtifact : Artifact
{
    /// <summary>
    /// Base64-encoded screenshot image.
    /// </summary>
    public string ImageData { get; init; } = string.Empty;

    /// <summary>
    /// Image format (e.g., "png", "jpeg").
    /// </summary>
    public string ImageFormat { get; init; } = "png";

    /// <summary>
    /// Width of the screenshot in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Height of the screenshot in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// URL or file path where the screenshot was captured.
    /// </summary>
    public string? SourceUrl { get; init; }

    /// <summary>
    /// Annotation or description of what the screenshot shows.
    /// </summary>
    public string? Annotation { get; init; }
}

/// <summary>
/// Browser recording artifact - recorded browser session.
/// </summary>
public record BrowserRecordingArtifact : Artifact
{
    /// <summary>
    /// URL to the recording file.
    /// </summary>
    public string RecordingUrl { get; init; } = string.Empty;

    /// <summary>
    /// Recording format (e.g., "webm", "mp4").
    /// </summary>
    public string Format { get; init; } = "webm";

    /// <summary>
    /// Duration of the recording in seconds.
    /// </summary>
    public int DurationSeconds { get; init; }

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Thumbnail image data (base64).
    /// </summary>
    public string? ThumbnailData { get; init; }

    /// <summary>
    /// Steps or actions performed in the recording.
    /// </summary>
    public List<string> Steps { get; init; } = new();
}
