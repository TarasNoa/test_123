using Libr4.Shared.Kernel.Domain;

namespace Libr4.Projects.Domain.Reports;

public enum ReportType
{
    ProjectSummary,
    TaskReport,
    MilestoneReport,
    ResourceReport,
    FinancialReport,
    TimeTrackingReport
}

public enum ReportStatus
{
    Draft,
    Generating,
    Completed,
    Failed
}

public class Report : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public ReportType Type { get; private set; }
    public Guid ProjectId { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public string? GeneratedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FilePath { get; private set; }
    public long FileSize { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Report() { }

    public static Report Create(string name, ReportType type, Guid projectId, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        return new Report
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            ProjectId = projectId,
            Status = ReportStatus.Draft,
            StartDate = startDate,
            EndDate = endDate,
            GeneratedBy = generatedBy,
            CreatedAt = DateTime.UtcNow,
            FileSize = 0
        };
    }

    public void StartGeneration()
    {
        if (Status == ReportStatus.Draft)
        {
            Status = ReportStatus.Generating;
        }
    }

    public void CompleteGeneration(string filePath, long fileSize)
    {
        Status = ReportStatus.Completed;
        FilePath = filePath;
        FileSize = fileSize;
        CompletedAt = DateTime.UtcNow;
    }

    public void FailGeneration(string errorMessage)
    {
        Status = ReportStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public void UpdateDateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

public class ReportTemplate : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public ReportType Type { get; private set; }
    public string? Description { get; private set; }
    public string TemplateContent { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ReportTemplate() { }

    public static ReportTemplate Create(string name, ReportType type, string templateContent, string? description = null, bool isDefault = false)
    {
        return new ReportTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Description = description,
            TemplateContent = templateContent,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateContent(string templateContent)
    {
        TemplateContent = templateContent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault(bool isDefault)
    {
        IsDefault = isDefault;
        UpdatedAt = DateTime.UtcNow;
    }
}
