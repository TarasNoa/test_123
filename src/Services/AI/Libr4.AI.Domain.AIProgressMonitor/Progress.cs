using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIProgressMonitor;

public enum ProgressStatus { Pending, Running, Completed, Failed, Cancelled }

public class Progress
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public float Percentage { get; set; }
    public ProgressStatus Status { get; set; } = ProgressStatus.Pending;
    public string? CurrentStep { get; set; }
    public int? TotalSteps { get; set; }
    public int? CompletedSteps { get; set; }
    public List<ProgressEvent> Events { get; set; } = new List<ProgressEvent>();
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EstimatedCompletionAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public void UpdateProgress(float pct, string? step = null)
    {
        Percentage = Math.Clamp(pct, 0, 100);
        CurrentStep = step;
    }
}

public class ProgressEvent
{
    public Guid Id { get; set; }
    public Guid ProgressId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset Timestamp { get; set; }
}
