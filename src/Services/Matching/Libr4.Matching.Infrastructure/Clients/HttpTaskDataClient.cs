using System.Net.Http.Json;
using Libr4.Matching.Application.Abstractions;

namespace Libr4.Matching.Infrastructure.Clients;

public sealed class HttpTaskDataClient : ITaskDataClient
{
    private readonly HttpClient _http;

    public HttpTaskDataClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<TaskData?> GetTaskAsync(Guid taskId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"/api/v1/tasks/{taskId}", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var dto = await response.Content.ReadFromJsonAsync<TaskApiResponse>(ct);
            if (dto is null)
                return null;

            return new TaskData(
                dto.Id,
                dto.Title,
                dto.Description,
                dto.Category,
                dto.Budget,
                dto.CreatedAt);
        }
        catch
        {
            return null;
        }
    }

    // Re-use the DTO shape from Tasks service (simplified mirror)
    private sealed record TaskApiResponse(
        Guid Id,
        string Title,
        string Description,
        string Category,
        decimal Budget,
        DateTimeOffset CreatedAt);
}
