using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.WorkDelivery;

public sealed class WorkDelivery : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid FreelancerId { get; private set; }
    public Guid ClientId { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public WorkDeliveryStatus Status { get; private set; }
    public PreviewType? PreviewType { get; private set; }
    public string? PreviewUrl { get; private set; }
    public string? PreviewContainerId { get; private set; }
    public DateTimeOffset? PreviewStartedAt { get; private set; }
    public DateTimeOffset? PreviewEndedAt { get; private set; }
    public int Version { get; private set; } = 1;
    public Guid? PreviousDeliveryId { get; private set; }
    public decimal? PaymentAmount { get; private set; }
    public string PaymentCurrency { get; private set; } = "USD";
    public string PaymentStatus { get; private set; } = "pending";
    public string? PaymentTransactionId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public bool AutoPayOnApproval { get; private set; }
    public bool RequireClientApproval { get; private set; } = true;
    public int MaxPreviewDurationMinutes { get; private set; } = 60;
    public Dictionary<string, object> ExtraData { get; private set; } = new();

    private readonly List<WorkDeliveryFile> _files = new();
    private readonly List<PreviewSession> _previewSessions = new();

    public IReadOnlyCollection<WorkDeliveryFile> Files => _files.AsReadOnly();
    public IReadOnlyCollection<PreviewSession> PreviewSessions => _previewSessions.AsReadOnly();

    private WorkDelivery() { }

    public static WorkDelivery Create(
        Guid taskId,
        Guid freelancerId,
        Guid clientId,
        string title,
        string? description,
        PreviewType? previewType,
        bool autoPayOnApproval,
        bool requireClientApproval,
        int maxPreviewDurationMinutes,
        DateTimeOffset now)
    {
        return new WorkDelivery
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            FreelancerId = freelancerId,
            ClientId = clientId,
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = WorkDeliveryStatus.Pending,
            PreviewType = previewType,
            Version = 1,
            AutoPayOnApproval = autoPayOnApproval,
            RequireClientApproval = requireClientApproval,
            MaxPreviewDurationMinutes = maxPreviewDurationMinutes,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Submit(DateTimeOffset now)
    {
        SubmittedAt = now;
        Status = WorkDeliveryStatus.Pending;
        UpdatedAt = now;
    }

    public void StartPreview(string previewUrl, string? containerId, DateTimeOffset now)
    {
        Status = WorkDeliveryStatus.PreviewActive;
        PreviewUrl = previewUrl;
        PreviewContainerId = containerId;
        PreviewStartedAt = now;
        UpdatedAt = now;
    }

    public void EndPreview(DateTimeOffset now)
    {
        Status = WorkDeliveryStatus.PreviewCompleted;
        PreviewEndedAt = now;
        UpdatedAt = now;
    }

    public void Approve(decimal paymentAmount, string currency, DateTimeOffset now)
    {
        Status = WorkDeliveryStatus.Approved;
        PaymentAmount = paymentAmount;
        PaymentCurrency = currency;
        ReviewedAt = now;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        Status = WorkDeliveryStatus.Rejected;
        ReviewedAt = now;
        UpdatedAt = now;
    }

    public void MarkAsPaid(string transactionId, DateTimeOffset now)
    {
        Status = WorkDeliveryStatus.Paid;
        PaymentStatus = "completed";
        PaymentTransactionId = transactionId;
        PaidAt = now;
        UpdatedAt = now;
    }

    public void CreateRevision(Guid newDeliveryId, DateTimeOffset now)
    {
        Version++;
        PreviousDeliveryId = Id;
        UpdatedAt = now;
    }

    public void AddFile(string filename, string originalFilename, string filePath, long fileSize, string mimeType, bool isEntryPoint, DateTimeOffset now)
    {
        var file = new WorkDeliveryFile(Id, filename, originalFilename, filePath, fileSize, mimeType, isEntryPoint, now);
        _files.Add(file);
        UpdatedAt = now;
    }

    public void AddPreviewSession(Guid clientId, string sessionToken, string previewUrl, DateTimeOffset now)
    {
        var session = new PreviewSession(Id, clientId, sessionToken, previewUrl, now);
        _previewSessions.Add(session);
        UpdatedAt = now;
    }

    public void UpdateExtraData(Dictionary<string, object> data, DateTimeOffset now)
    {
        ExtraData = data ?? new();
        UpdatedAt = now;
    }

    public int GetDaysSinceSubmission(DateTimeOffset now)
    {
        if (!SubmittedAt.HasValue)
            return 0;
        return (int)(now - SubmittedAt.Value).TotalDays;
    }

    public bool IsOverdue(DateTimeOffset now)
    {
        if (!SubmittedAt.HasValue)
            return false;
        var daysElapsed = GetDaysSinceSubmission(now);
        return daysElapsed > 7 && Status == WorkDeliveryStatus.Pending;
    }
}

public sealed class WorkDeliveryFile
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DeliveryId { get; private set; }
    public string Filename { get; private set; } = "";
    public string OriginalFilename { get; private set; } = "";
    public string FilePath { get; private set; } = "";
    public long FileSize { get; private set; }
    public string MimeType { get; private set; } = "";
    public string? FileCategory { get; private set; }
    public bool IsEntryPoint { get; private set; }
    public bool IsScanned { get; private set; }
    public string? ScanResult { get; private set; }
    public Dictionary<string, object> ScanDetails { get; private set; } = new();
    public string? ContentPreview { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }

    private WorkDeliveryFile() { }

    internal WorkDeliveryFile(Guid deliveryId, string filename, string originalFilename, string filePath, long fileSize, string mimeType, bool isEntryPoint, DateTimeOffset now)
    {
        DeliveryId = deliveryId;
        Filename = filename.Trim();
        OriginalFilename = originalFilename.Trim();
        FilePath = filePath.Trim();
        FileSize = fileSize;
        MimeType = mimeType.Trim();
        IsEntryPoint = isEntryPoint;
        UploadedAt = now;
    }

    public void MarkAsScanned(string result, Dictionary<string, object>? details = null)
    {
        IsScanned = true;
        ScanResult = result;
        ScanDetails = details ?? new();
    }

    public void SetContentPreview(string preview)
    {
        ContentPreview = preview;
    }
}

public sealed class PreviewSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DeliveryId { get; private set; }
    public Guid ClientId { get; private set; }
    public string SessionToken { get; private set; } = "";
    public string Status { get; private set; } = "starting";
    public string? PreviewUrl { get; private set; }
    public string? WebsocketUrl { get; private set; }
    public string? ContainerId { get; private set; }
    public string? ContainerName { get; private set; }
    public int? Port { get; private set; }
    public string CpuLimit { get; private set; } = "1.0";
    public string MemoryLimit { get; private set; } = "512m";
    public int MaxDurationMinutes { get; private set; } = 60;
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public int InteractionsCount { get; private set; }
    public List<Dictionary<string, object>> InteractionsLog { get; private set; } = new();
    public string? ClientNotes { get; private set; }
    public int? Rating { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, object> ErrorDetails { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PreviewSession() { }

    internal PreviewSession(Guid deliveryId, Guid clientId, string sessionToken, string previewUrl, DateTimeOffset now)
    {
        DeliveryId = deliveryId;
        ClientId = clientId;
        SessionToken = sessionToken.Trim();
        PreviewUrl = previewUrl.Trim();
        CreatedAt = now;
        UpdatedAt = now;
        LastActivityAt = now;
    }

    public void Start(string? containerId, string? containerName, int? port, DateTimeOffset now)
    {
        Status = "running";
        ContainerId = containerId;
        ContainerName = containerName;
        Port = port;
        StartedAt = now;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void Stop(DateTimeOffset now)
    {
        Status = "stopped";
        EndedAt = now;
        UpdatedAt = now;
    }

    public void RecordInteraction(string type, string? selector, string? pageUrl, Dictionary<string, object>? data, DateTimeOffset now)
    {
        InteractionsCount++;
        InteractionsLog.Add(new Dictionary<string, object>
        {
            { "type", type },
            { "selector", selector ?? "" },
            { "pageUrl", pageUrl ?? "" },
            { "timestamp", now },
            { "data", (object)(data ?? new()) }
        });
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void SetError(string message, Dictionary<string, object>? details = null, DateTimeOffset? now = null)
    {
        Status = "error";
        ErrorMessage = message;
        ErrorDetails = details ?? new();
        if (now.HasValue)
            UpdatedAt = now.Value;
    }

    public void SetClientFeedback(string? notes, int? rating, DateTimeOffset now)
    {
        ClientNotes = notes?.Trim();
        Rating = rating;
        UpdatedAt = now;
    }

    public bool IsExpired(DateTimeOffset now)
    {
        if (!StartedAt.HasValue)
            return false;
        var durationMinutes = (int)(now - StartedAt.Value).TotalMinutes;
        return durationMinutes > MaxDurationMinutes;
    }
}

public enum WorkDeliveryStatus
{
    Pending = 0,
    PreviewActive = 1,
    PreviewCompleted = 2,
    Approved = 3,
    Rejected = 4,
    Paid = 5
}

public enum PreviewType
{
    WebStatic = 0,
    WebReact = 1,
    WebVue = 2,
    WebAngular = 3,
    Python = 4,
    NodeJs = 5,
    MobileFlutter = 6,
    MobileReactNative = 7,
    Docker = 8
}
