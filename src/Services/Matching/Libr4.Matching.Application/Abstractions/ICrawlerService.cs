namespace Libr4.Matching.Application.Abstractions;

public record ExternalJobDto(
    string Id,
    string Source,
    string SourceUrl,
    string Title,
    string Company,
    string Location,
    bool IsRemote,
    string DescriptionClean,
    IReadOnlyList<string> Skills,
    long SalaryMin,
    long SalaryMax,
    string Currency,
    string PostedAt);

public interface ICrawlerService
{
    Task<IReadOnlyList<ExternalJobDto>> FetchJobsAsync(
        string source,
        string query,
        string location,
        int maxResults,
        CancellationToken ct = default);

    IAsyncEnumerable<ExternalJobDto> StreamJobsAsync(
        string source,
        string query,
        int maxResults,
        CancellationToken ct = default);
}
