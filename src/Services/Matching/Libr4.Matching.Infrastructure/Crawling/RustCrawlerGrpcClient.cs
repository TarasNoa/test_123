using Grpc.Net.Client;
using Libr4.Matching.Application.Abstractions;
using Libr4.Matching.Infrastructure.Crawling;
using Microsoft.Extensions.Logging;

namespace Libr4.Matching.Infrastructure.Crawling;

public sealed class RustCrawlerGrpcClient : ICrawlerService
{
    private readonly CrawlerService.CrawlerServiceClient _client;
    private readonly ILogger<RustCrawlerGrpcClient> _logger;

    public RustCrawlerGrpcClient(GrpcChannel channel, ILogger<RustCrawlerGrpcClient> logger)
    {
        _client = new CrawlerService.CrawlerServiceClient(channel);
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalJobDto>> FetchJobsAsync(
        string source,
        string query,
        string location,
        int maxResults,
        CancellationToken ct = default)
    {
        var response = await _client.FetchJobsAsync(new FetchJobsRequest
        {
            Source = source,
            Query = query,
            Location = location,
            MaxResults = maxResults,
        }, cancellationToken: ct);

        return response.Jobs.Select(ToDto).ToList();
    }

    public async IAsyncEnumerable<ExternalJobDto> StreamJobsAsync(
        string source,
        string query,
        int maxResults,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var stream = _client.StreamJobs(new FetchJobsRequest
        {
            Source = source,
            Query = query,
            MaxResults = maxResults,
        }, cancellationToken: ct);

        await foreach (var job in stream.ResponseStream.ReadAllAsync(ct))
        {
            yield return ToDto(job);
        }
    }

    private static ExternalJobDto ToDto(JobListing j) => new(
        Id: j.Id,
        Source: j.Source,
        SourceUrl: j.SourceUrl,
        Title: j.Title,
        Company: j.Company,
        Location: j.Location,
        IsRemote: j.IsRemote,
        DescriptionClean: j.DescriptionClean,
        Skills: j.Skills.ToList(),
        SalaryMin: j.Salary?.Min ?? 0,
        SalaryMax: j.Salary?.Max ?? 0,
        Currency: j.Salary?.Currency ?? string.Empty,
        PostedAt: j.PostedAt);
}
