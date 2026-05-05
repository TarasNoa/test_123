using Libr4.Shared.Kernel.Domain;

namespace Libr4.DevOps.Domain.Infrastructure;

public enum PipelineStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public enum PipelineTrigger
{
    Manual,
    Push,
    PullRequest,
    Scheduled,
    Webhook
}

public class CiCdPipeline : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string RepositoryUrl { get; private set; } = string.Empty;
    public string Branch { get; private set; } = "main";
    public PipelineStatus Status { get; private set; }
    public PipelineTrigger Trigger { get; private set; }
    public Guid? TriggeredByUserId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int DurationSeconds { get; private set; }
    public string? CommitHash { get; private set; }
    public string? CommitMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<PipelineStage> _stages = new();
    public IReadOnlyCollection<PipelineStage> Stages => _stages.AsReadOnly();

    private readonly List<PipelineArtifact> _artifacts = new();
    public IReadOnlyCollection<PipelineArtifact> Artifacts => _artifacts.AsReadOnly();

    private CiCdPipeline() { }

    public static CiCdPipeline Create(
        string name,
        string repositoryUrl,
        string branch,
        PipelineTrigger trigger,
        Guid? triggeredByUserId = null)
    {
        return new CiCdPipeline
        {
            Id = Guid.NewGuid(),
            Name = name,
            RepositoryUrl = repositoryUrl,
            Branch = branch,
            Status = PipelineStatus.Pending,
            Trigger = trigger,
            TriggeredByUserId = triggeredByUserId,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Start()
    {
        Status = PipelineStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Succeed()
    {
        Status = PipelineStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string? errorMessage = null)
    {
        Status = PipelineStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = PipelineStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddStage(PipelineStage stage)
    {
        _stages.Add(stage);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddArtifact(PipelineArtifact artifact)
    {
        _artifacts.Add(artifact);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCommitInfo(string commitHash, string commitMessage)
    {
        CommitHash = commitHash;
        CommitMessage = commitMessage;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class PipelineStage : Entity<Guid>
{
    public Guid PipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PipelineStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public int DurationSeconds { get; private set; }
    public string? Log { get; private set; }
    public int Order { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PipelineStage() { }

    public static PipelineStage Create(Guid pipelineId, string name, int order)
    {
        return new PipelineStage
        {
            Id = Guid.NewGuid(),
            PipelineId = pipelineId,
            Name = name,
            Status = PipelineStatus.Pending,
            StartedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Order = order
        };
    }

    public void Start()
    {
        Status = PipelineStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Succeed(string? log = null)
    {
        Status = PipelineStatus.Succeeded;
        CompletedAt = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        Log = log;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string log)
    {
        Status = PipelineStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        DurationSeconds = (int)(CompletedAt.Value - StartedAt).TotalSeconds;
        Log = log;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class PipelineArtifact : Entity<Guid>
{
    public Guid PipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string? ContentType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PipelineArtifact() { }

    public static PipelineArtifact Create(Guid pipelineId, string name, string path, long sizeBytes, string? contentType = null)
    {
        return new PipelineArtifact
        {
            Id = Guid.NewGuid(),
            PipelineId = pipelineId,
            Name = name,
            Path = path,
            SizeBytes = sizeBytes,
            ContentType = contentType,
            CreatedAt = DateTime.UtcNow
        };
    }
}
