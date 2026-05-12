namespace Libr4.Matching.Application.Abstractions;

public record VectorSearchResult(
    Guid Id,
    float Score,
    Dictionary<string, string> Payload,
    float[]? Embedding = null);

public record SearchFilter(
    float? MinRating = null,
    int? MinCompletedTasks = null,
    List<string>? RequiredSkills = null);

public interface IVectorIndex
{
    Task EnsureCollectionsAsync(CancellationToken ct = default);

    Task UpsertFreelancerAsync(
        Guid freelancerId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default);

    Task UpsertTaskAsync(
        Guid taskId,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken ct = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchFreelancersAsync(
        float[] queryEmbedding,
        int topK = 20,
        float minScore = 0.35f,
        SearchFilter? filter = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchTasksAsync(
        float[] queryEmbedding,
        int topK = 20,
        float minScore = 0.35f,
        CancellationToken ct = default);
}
