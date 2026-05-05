using System;
using System.Collections.Generic;

namespace Libr4.AI.Domain.AIVideoGenerator;

public enum VideoGenerationStatus { Pending, Generating, Completed, Failed }
public enum VideoStyle { Realistic, Animated, Cartoon, Abstract, Cinematic }

public class VideoGeneration
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public VideoStyle Style { get; set; } = VideoStyle.Realistic;
    public int DurationSeconds { get; set; } = 5;
    public string Resolution { get; set; } = "1080p";
    public int FramesPerSecond { get; set; } = 30;
    public VideoGenerationStatus Status { get; set; } = VideoGenerationStatus.Pending;
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public float? ProgressPercentage { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Cost { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class VideoFrame
{
    public Guid Id { get; set; }
    public Guid GenerationId { get; set; }
    public int FrameNumber { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
