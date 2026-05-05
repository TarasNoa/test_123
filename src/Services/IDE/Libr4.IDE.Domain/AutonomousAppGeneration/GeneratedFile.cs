namespace Libr4.IDE.Domain.AutonomousAppGeneration;

/// <summary>
/// A source file produced by a generation/fixing iteration. The content lives
/// in the shadow workspace; only metadata and latest content are tracked here.
/// </summary>
public sealed class GeneratedFile
{
    public string RelativePath { get; }
    public string Language { get; }
    public string Content { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public GeneratedFile(string relativePath, string language, string content)
    {
        RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
        Language = language ?? "text";
        Content = content ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string content)
    {
        Content = content ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }
}
