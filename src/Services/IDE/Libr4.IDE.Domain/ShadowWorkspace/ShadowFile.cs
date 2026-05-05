namespace Libr4.IDE.Domain.ShadowWorkspace;

/// <summary>
/// Entity representing a file in shadow workspace
/// </summary>
public class ShadowFile
{
    public Guid Id { get; private set; }
    public string FilePath { get; private set; }
    public string Content { get; private set; }
    public string Status { get; private set; }
    public List<ValidationResult> ValidationResults { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private ShadowFile() { }
    
    public ShadowFile(
        string filePath,
        string content)
    {
        Id = Guid.NewGuid();
        FilePath = filePath;
        Content = content;
        Status = "pending";
        ValidationResults = new List<ValidationResult>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetStatus(string status)
    {
        Status = status;
    }
    
    public void AddValidationResult(ValidationResult result)
    {
        if (result != null)
        {
            ValidationResults.Add(result);
        }
    }
    
    public static ShadowFile Create(
        string filePath,
        string content)
    {
        return new ShadowFile(filePath, content);
    }
}
