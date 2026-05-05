using Libr4.Shared.Kernel.Domain;

namespace Libr4.Tasks.Domain.DisputeResolution;

public sealed class Dispute : AggregateRoot<Guid>
{
    public Guid TaskId { get; private set; }
    public Guid InitiatorId { get; private set; }
    public Guid RespondentId { get; private set; }
    public DisputeCategory Category { get; private set; }
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal? DisputeAmount { get; private set; }
    public string ResolutionRequested { get; private set; } = "";
    public DisputeStatus Status { get; private set; }
    public DisputeSeverity Severity { get; private set; }
    public DisputePriority Priority { get; private set; }
    public Guid? AssignedModeratorId { get; private set; }
    public Guid? AssignedArbitratorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastActivityAt { get; private set; }
    public DateTimeOffset? ModeratorAssignedAt { get; private set; }
    public DateTimeOffset? EscalatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? ResolutionProposedAt { get; private set; }
    public Guid? ResolutionId { get; private set; }
    public string? FinalOutcome { get; private set; }
    public Dictionary<string, object> AiAnalysis { get; private set; } = new();
    public float? AiConfidence { get; private set; }
    public List<string> EvidenceFiles { get; private set; } = new();
    public string? EscalationReason { get; private set; }
    public string? DismissalReason { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<DisputeMessage> _messages = new();
    private readonly List<DisputeEvidence> _evidence = new();
    private readonly List<DisputeResolution> _resolutions = new();
    private readonly List<DisputeArbitrator> _arbitrators = new();

    public IReadOnlyCollection<DisputeMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<DisputeEvidence> Evidence => _evidence.AsReadOnly();
    public IReadOnlyCollection<DisputeResolution> Resolutions => _resolutions.AsReadOnly();
    public IReadOnlyCollection<DisputeArbitrator> Arbitrators => _arbitrators.AsReadOnly();

    private Dispute() { }

    public static Dispute Create(
        Guid taskId,
        Guid initiatorId,
        Guid respondentId,
        DisputeCategory category,
        string title,
        string description,
        decimal? amount,
        string resolutionRequested,
        DisputeSeverity severity,
        DisputePriority priority,
        DateTimeOffset now)
    {
        return new Dispute
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            InitiatorId = initiatorId,
            RespondentId = respondentId,
            Category = category,
            Title = title.Trim(),
            Description = description.Trim(),
            DisputeAmount = amount,
            ResolutionRequested = resolutionRequested.Trim(),
            Status = DisputeStatus.Open,
            Severity = severity,
            Priority = priority,
            CreatedAt = now,
            LastActivityAt = now,
            UpdatedAt = now
        };
    }

    public void AssignModerator(Guid moderatorId, DateTimeOffset now)
    {
        AssignedModeratorId = moderatorId;
        ModeratorAssignedAt = now;
        Status = DisputeStatus.UnderReview;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AssignArbitrator(Guid arbitratorId, DateTimeOffset now)
    {
        AssignedArbitratorId = arbitratorId;
        Status = DisputeStatus.Arbitration;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void ProposeResolution(DateTimeOffset now)
    {
        Status = DisputeStatus.ResolutionProposed;
        ResolutionProposedAt = now;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void Resolve(string outcome, Guid resolutionId, DateTimeOffset now)
    {
        Status = DisputeStatus.Resolved;
        FinalOutcome = outcome;
        ResolutionId = resolutionId;
        ResolvedAt = now;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void Escalate(string reason, DateTimeOffset now)
    {
        Status = DisputeStatus.Escalated;
        EscalationReason = reason;
        EscalatedAt = now;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void Dismiss(string reason, DateTimeOffset now)
    {
        Status = DisputeStatus.Dismissed;
        DismissalReason = reason;
        ResolvedAt = now;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddMessage(Guid senderId, string message, string messageType, bool isPrivate, bool isOfficial, DateTimeOffset now)
    {
        var msg = new DisputeMessage(Id, senderId, message, messageType, isPrivate, isOfficial, now);
        _messages.Add(msg);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddEvidence(Guid submittedBy, string evidenceType, string? evidenceData, string? description, string? fileName, long? fileSize, string? fileType, string? fileHash, DateTimeOffset now)
    {
        var evidence = new DisputeEvidence(Id, submittedBy, evidenceType, evidenceData, description, fileName, fileSize, fileType, fileHash, now);
        _evidence.Add(evidence);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddResolution(Guid proposerId, string resolutionType, string terms, decimal? refund, decimal? penalty, decimal? compensation, DateTimeOffset now)
    {
        var resolution = new DisputeResolution(Id, proposerId, resolutionType, terms, refund, penalty, compensation, now);
        _resolutions.Add(resolution);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void AddArbitrator(Guid arbitratorId, Guid? assignedBy, string? specialization, string? experienceLevel, decimal? feeRate, DateTimeOffset now)
    {
        var arb = new DisputeArbitrator(Id, arbitratorId, assignedBy, specialization, experienceLevel, feeRate, now);
        _arbitrators.Add(arb);
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public void SetAiAnalysis(Dictionary<string, object> analysis, float confidence, DateTimeOffset now)
    {
        AiAnalysis = analysis ?? new();
        AiConfidence = confidence;
        LastActivityAt = now;
        UpdatedAt = now;
    }

    public int GetDaysOpen(DateTimeOffset now)
    {
        return (int)(now - CreatedAt).TotalDays;
    }

    public bool RequiresEscalation(DateTimeOffset now)
    {
        var daysOpen = GetDaysOpen(now);
        var maxDuration = Severity switch
        {
            DisputeSeverity.Low => 30,
            DisputeSeverity.Medium => 21,
            DisputeSeverity.High => 14,
            DisputeSeverity.Critical => 7,
            _ => 30
        };
        return daysOpen > maxDuration || Severity is DisputeSeverity.High or DisputeSeverity.Critical;
    }

    public bool IsResolved => Status == DisputeStatus.Resolved;
}

public sealed class DisputeMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DisputeId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Message { get; private set; } = "";
    public string MessageType { get; private set; } = "communication";
    public List<string> EvidenceFiles { get; private set; } = new();
    public Dictionary<string, object> Attachments { get; private set; } = new();
    public bool IsPrivate { get; private set; }
    public bool IsOfficial { get; private set; }
    public Guid? ParentMessageId { get; private set; }
    public string Status { get; private set; } = "sent";
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private DisputeMessage() { }

    internal DisputeMessage(Guid disputeId, Guid senderId, string message, string messageType, bool isPrivate, bool isOfficial, DateTimeOffset now)
    {
        DisputeId = disputeId;
        SenderId = senderId;
        Message = message.Trim();
        MessageType = messageType;
        IsPrivate = isPrivate;
        IsOfficial = isOfficial;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void MarkAsRead(DateTimeOffset now)
    {
        Status = "read";
        ReadAt = now;
        UpdatedAt = now;
    }
}

public sealed class DisputeEvidence
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DisputeId { get; private set; }
    public Guid SubmittedBy { get; private set; }
    public string EvidenceType { get; private set; } = "";
    public string? EvidenceData { get; private set; }
    public string? Description { get; private set; }
    public string? FileName { get; private set; }
    public long? FileSize { get; private set; }
    public string? FileType { get; private set; }
    public string? FileHash { get; private set; }
    public bool IsVerified { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public string? VerificationNotes { get; private set; }
    public bool IsAdmissible { get; private set; } = true;
    public string? InadmissibilityReason { get; private set; }
    public string Status { get; private set; } = "submitted";
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    private DisputeEvidence() { }

    internal DisputeEvidence(Guid disputeId, Guid submittedBy, string evidenceType, string? evidenceData, string? description, string? fileName, long? fileSize, string? fileType, string? fileHash, DateTimeOffset now)
    {
        DisputeId = disputeId;
        SubmittedBy = submittedBy;
        EvidenceType = evidenceType;
        EvidenceData = evidenceData;
        Description = description;
        FileName = fileName;
        FileSize = fileSize;
        FileType = fileType;
        FileHash = fileHash;
        SubmittedAt = now;
    }

    public void Verify(Guid verifiedBy, string notes, DateTimeOffset now)
    {
        IsVerified = true;
        VerifiedBy = verifiedBy;
        VerifiedAt = now;
        VerificationNotes = notes;
        Status = "reviewed";
        ReviewedAt = now;
    }

    public void MarkAsAdmissible()
    {
        IsAdmissible = true;
        InadmissibilityReason = null;
        Status = "accepted";
    }

    public void MarkAsInadmissible(string reason)
    {
        IsAdmissible = false;
        InadmissibilityReason = reason;
        Status = "rejected";
    }
}

public sealed class DisputeResolution
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DisputeId { get; private set; }
    public Guid ProposerId { get; private set; }
    public string ResolutionType { get; private set; } = "";
    public string ResolutionTerms { get; private set; } = "";
    public decimal? AmountRefund { get; private set; }
    public decimal? AmountPenalty { get; private set; }
    public decimal? AmountCompensation { get; private set; }
    public List<string> AdditionalActions { get; private set; } = new();
    public Dictionary<string, object> Deadlines { get; private set; } = new();
    public string? Response { get; private set; }
    public Guid? ResponderId { get; private set; }
    public string? CounterTerms { get; private set; }
    public string? ResponseReason { get; private set; }
    public string Status { get; private set; } = "proposed";
    public Dictionary<string, object> AiAnalysis { get; private set; } = new();
    public int? FairnessScore { get; private set; }
    public int? AcceptanceLikelihood { get; private set; }
    public DateTimeOffset ProposedAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }

    private DisputeResolution() { }

    internal DisputeResolution(Guid disputeId, Guid proposerId, string resolutionType, string terms, decimal? refund, decimal? penalty, decimal? compensation, DateTimeOffset now)
    {
        DisputeId = disputeId;
        ProposerId = proposerId;
        ResolutionType = resolutionType;
        ResolutionTerms = terms;
        AmountRefund = refund;
        AmountPenalty = penalty;
        AmountCompensation = compensation;
        ProposedAt = now;
    }

    public void Accept(Guid responderId, DateTimeOffset now)
    {
        Response = "accept";
        ResponderId = responderId;
        Status = "accepted";
        RespondedAt = now;
        ExecutedAt = now;
    }

    public void Reject(Guid responderId, string reason, DateTimeOffset now)
    {
        Response = "reject";
        ResponderId = responderId;
        ResponseReason = reason;
        Status = "rejected";
        RespondedAt = now;
    }

    public void Counter(Guid responderId, string counterTerms, DateTimeOffset now)
    {
        Response = "counter";
        ResponderId = responderId;
        CounterTerms = counterTerms;
        Status = "countered";
        RespondedAt = now;
    }

    public bool IsExpired(DateTimeOffset now)
    {
        return ExpiresAt.HasValue && now > ExpiresAt.Value;
    }

    public bool IsPending(DateTimeOffset now)
    {
        return Status == "proposed" && !IsExpired(now);
    }
}

public sealed class DisputeArbitrator
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DisputeId { get; private set; }
    public Guid ArbitratorId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedBy { get; private set; }
    public string? AssignmentReason { get; private set; }
    public string? Specialization { get; private set; }
    public string? ExperienceLevel { get; private set; }
    public decimal? FeeRate { get; private set; }
    public string Status { get; private set; } = "assigned";
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? Decision { get; private set; }
    public string? DecisionReasoning { get; private set; }
    public int? ConfidenceLevel { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private DisputeArbitrator() { }

    internal DisputeArbitrator(Guid disputeId, Guid arbitratorId, Guid? assignedBy, string? specialization, string? experienceLevel, decimal? feeRate, DateTimeOffset now)
    {
        DisputeId = disputeId;
        ArbitratorId = arbitratorId;
        AssignedBy = assignedBy;
        Specialization = specialization;
        ExperienceLevel = experienceLevel;
        FeeRate = feeRate;
        AssignedAt = now;
        UpdatedAt = now;
    }

    public void Accept(DateTimeOffset now)
    {
        Status = "active";
        AcceptedAt = now;
        UpdatedAt = now;
    }

    public void Complete(string decision, string reasoning, int confidenceLevel, DateTimeOffset now)
    {
        Status = "completed";
        Decision = decision;
        DecisionReasoning = reasoning;
        ConfidenceLevel = confidenceLevel;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void Withdraw(DateTimeOffset now)
    {
        Status = "withdrawn";
        UpdatedAt = now;
    }
}

public enum DisputeStatus
{
    Open = 0,
    UnderReview = 1,
    Mediation = 2,
    Arbitration = 3,
    ResolutionProposed = 4,
    CounterProposed = 5,
    Resolved = 6,
    Escalated = 7,
    Dismissed = 8
}

public enum DisputeCategory
{
    PaymentIssue = 0,
    WorkQuality = 1,
    DeliveryDelay = 2,
    Communication = 3,
    ScopeDisagreement = 4,
    Fraud = 5,
    Harassment = 6,
    IntellectualProperty = 7,
    ContractBreach = 8,
    Other = 9
}

public enum DisputeSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum DisputePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}
