using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Application.ML;

namespace Libr4.AI.Application;

public class TaskRecommendationService : ITaskRecommendationService
{
    private readonly IRustInferenceBridge _rustBridge;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public TaskRecommendationService(IRustInferenceBridge rustBridge)
    {
        _rustBridge = rustBridge;
    }

    public async Task<List<TaskRecommendationResult>> RecommendTasksAsync(TaskRecommendationRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || request.UserProfile == null || request.AvailableTasks == null)
        {
            return new List<TaskRecommendationResult>();
        }

        var payload = new
        {
            type = "taskRecommendations",
            request
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var resultJson = await _rustBridge.RunInferenceAsync(json);

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return new List<TaskRecommendationResult>();
        }

        try
        {
            var results = JsonSerializer.Deserialize<List<TaskRecommendationResult>>(resultJson, _jsonOptions);
            return results ?? new List<TaskRecommendationResult>();
        }
        catch (JsonException)
        {
            // fallback to empty list if deserialization fails
            return new List<TaskRecommendationResult>();
        }
    }
}