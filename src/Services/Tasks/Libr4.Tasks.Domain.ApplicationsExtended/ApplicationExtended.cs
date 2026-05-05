using System;
using System.Collections.Generic;

namespace Libr4.Tasks.Domain.ApplicationsExtended;

/// <summary>Proposal status enumeration</summary>
public enum ProposalStatus
{
    Draft,
    Submitted,
    Accepted,
    Rejected,
    Withdrawn,
    Completed
}

/// <summary>Milestone status enumeration</summary>
public enum MilestoneStatus
{
    Pending,
    InProgress,
    Completed,
    Disputed,
    Cancelled
}

/// <summary>Attachment type enumeration</summary>
public enum AttachmentType
{
    Document,
    Image,
    Video,
    Code,
    Archive,
    Other
}

/// <summary>Proposal milestone entity</summary>
public class ProposalMilestone
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public List<string> Deliverables { get; set; } = [];
    public int Order { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsOverdue() =>
        Status == MilestoneStatus.Pending && DateTimeOffset.UtcNow > DueDate;

    public void Complete(DateTimeOffset now)
    {
        Status = MilestoneStatus.Completed;
        UpdatedAt = now;
    }
}

/// <summary>Proposal attachment entity</summary>
public class ProposalAttachment
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public AttachmentType Type { get; set; }
    public string? MimeType { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Video pitch entity</summary>
public class VideoPitch
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int DurationSeconds { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Extended application/proposal aggregate root</summary>
public class ApplicationExtended
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ApplicantId { get; set; }
    
    // Базовая информация
    public string Proposal { get; set; } = string.Empty;
    public decimal? BidAmount { get; set; }
    public int? EstimatedDurationDays { get; set; }
    public string? CoverLetter { get; set; }
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    
    // Статус скрининга
    public float? ScreeningScore { get; set; }
    public string? ScreeningStatus { get; set; }  // passed, failed, review, manual
    public string? ScreeningComment { get; set; }
    public DateTimeOffset? ScreenedAt { get; set; }
    
    // Расширенные возможности
    public List<ProposalMilestone> Milestones { get; set; } = [];
    public List<ProposalAttachment> Attachments { get; set; } = [];
    public VideoPitch? VideoPitch { get; set; }
    
    // Метаданные
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void Submit(DateTimeOffset now)
    {
        if (Status != ProposalStatus.Draft)
            throw new InvalidOperationException("Only draft proposals can be submitted");
        Status = ProposalStatus.Submitted;
        UpdatedAt = now;
    }

    public void Accept(DateTimeOffset now)
    {
        Status = ProposalStatus.Accepted;
        UpdatedAt = now;
    }

    public void Reject(DateTimeOffset now)
    {
        Status = ProposalStatus.Rejected;
        UpdatedAt = now;
    }

    public void Withdraw(DateTimeOffset now)
    {
        if (Status == ProposalStatus.Accepted || Status == ProposalStatus.Completed)
            throw new InvalidOperationException("Cannot withdraw accepted or completed proposals");
        Status = ProposalStatus.Withdrawn;
        UpdatedAt = now;
    }

    public void Complete(DateTimeOffset now)
    {
        Status = ProposalStatus.Completed;
        UpdatedAt = now;
    }

    public void AddMilestone(string title, string? description, decimal amount, DateTimeOffset dueDate, List<string> deliverables, DateTimeOffset now)
    {
        var milestone = new ProposalMilestone
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            Title = title,
            Description = description,
            Amount = amount,
            DueDate = dueDate,
            Status = MilestoneStatus.Pending,
            Deliverables = deliverables,
            Order = Milestones.Count + 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        Milestones.Add(milestone);
        UpdatedAt = now;
    }

    public void AddAttachment(string fileName, string filePath, long fileSizeBytes, AttachmentType type, string? mimeType, string? description, DateTimeOffset now)
    {
        var attachment = new ProposalAttachment
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            FileName = fileName,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            Type = type,
            MimeType = mimeType,
            Description = description,
            CreatedAt = now
        };
        Attachments.Add(attachment);
        UpdatedAt = now;
    }

    public void SetVideoPitch(string videoUrl, string? thumbnailUrl, int durationSeconds, string title, string? description, DateTimeOffset now)
    {
        VideoPitch = new VideoPitch
        {
            Id = Guid.NewGuid(),
            ProposalId = Id,
            VideoUrl = videoUrl,
            ThumbnailUrl = thumbnailUrl,
            DurationSeconds = durationSeconds,
            Title = title,
            Description = description,
            IsPublic = false,
            CreatedAt = now,
            UpdatedAt = now
        };
        UpdatedAt = now;
    }

    public decimal GetTotalMilestoneAmount() =>
        Milestones.Sum(m => m.Amount);

    public int GetCompletedMilestonesCount() =>
        Milestones.Count(m => m.Status == MilestoneStatus.Completed);

    public bool AllMilestonesCompleted() =>
        Milestones.Count > 0 && Milestones.All(m => m.Status == MilestoneStatus.Completed);
}
