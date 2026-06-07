namespace Libr4.IDE.Application.AutonomousAppGeneration.Services.PlatformUtilization;

/// <summary>Resolved stack tags used to filter platform capabilities.</summary>
public sealed class PlatformStackProfile
{
    public required string Summary { get; init; }
    public IReadOnlyList<string> SkillIds { get; init; } = Array.Empty<string>();
    public bool IsPython { get; init; }
    public bool IsFastApi { get; init; }
    public bool IsDjango { get; init; }
    public bool IsJava { get; init; }
    public bool IsSpring { get; init; }
    public bool IsDotNet { get; init; }
    public bool IsNode { get; init; }
    public bool IsReact { get; init; }
    public bool IsNext { get; init; }
    public bool UsesPytest { get; init; }

    public static PlatformStackProfile FromBlob(string blob)
    {
        var lower = blob.ToLowerInvariant();
        var skills = new List<string>();
        if (lower.Contains("fastapi")) skills.Add("python-fastapi");
        if (lower.Contains("django")) skills.Add("python-django");
        if (lower.Contains("spring") || lower.Contains("java")) skills.Add("java-spring");
        if (lower.Contains("express")) skills.Add("js-express");
        if (lower.Contains("nestjs") || lower.Contains("nest")) skills.Add("ts-nestjs");
        if (lower.Contains("next")) skills.Add("ts-react");
        else if (lower.Contains("react")) skills.Add("js-react");
        if (lower.Contains("dotnet") || lower.Contains("asp.net")) skills.Add("csharp-aspnet");

        var isPython = lower.Contains("python") || lower.Contains("fastapi") || lower.Contains("django");
        return new PlatformStackProfile
        {
            Summary = string.IsNullOrWhiteSpace(blob) ? "(unknown — infer from user request)" : blob.Trim(),
            SkillIds = skills,
            IsPython = isPython,
            IsFastApi = lower.Contains("fastapi"),
            IsDjango = lower.Contains("django"),
            IsJava = lower.Contains("java"),
            IsSpring = lower.Contains("spring"),
            IsDotNet = lower.Contains("dotnet") || lower.Contains("asp.net") || lower.Contains("c#"),
            IsNode = lower.Contains("node") || lower.Contains("express") || lower.Contains("nestjs"),
            IsReact = lower.Contains("react"),
            IsNext = lower.Contains("next"),
            UsesPytest = isPython && (lower.Contains("pytest") || lower.Contains("fastapi") || lower.Contains("test"))
        };
    }
}
