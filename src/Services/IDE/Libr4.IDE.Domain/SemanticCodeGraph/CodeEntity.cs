namespace Libr4.IDE.Domain.SemanticCodeGraph;

/// <summary>
/// Entity representing a code entity
/// </summary>
public class CodeEntity
{
    public Guid Id { get; private set; }
    public string EntityType { get; private set; }
    public string Name { get; private set; }
    public string FilePath { get; private set; }
    public float[] Embedding { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private CodeEntity() { }
    
    public CodeEntity(
        string entityType,
        string name,
        string filePath,
        float[] embedding,
        Dictionary<string, object>? metadata = null)
    {
        Id = Guid.NewGuid();
        EntityType = entityType;
        Name = name;
        FilePath = filePath;
        Embedding = embedding;
        Metadata = metadata ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetEmbedding(float[] embedding)
    {
        Embedding = embedding;
    }
    
    public void AddMetadata(string key, object value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Metadata[key] = value;
        }
    }
    
    public static CodeEntity Create(
        string entityType,
        string name,
        string filePath,
        float[] embedding,
        Dictionary<string, object>? metadata = null)
    {
        return new CodeEntity(entityType, name, filePath, embedding, metadata);
    }
}
