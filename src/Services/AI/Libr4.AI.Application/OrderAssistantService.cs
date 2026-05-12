using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Libr4.AI.Application.Abstractions;
using Libr4.AI.Application.ML;

namespace Libr4.AI.Application;

public class OrderAssistantService : IOrderAssistantService
{
    private readonly IRustInferenceBridge _rustBridge;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public OrderAssistantService(IRustInferenceBridge rustBridge)
    {
        _rustBridge = rustBridge;
    }

    public async Task<OrderAssistantResult> SuggestOrderAsync(OrderAssistantRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TaskTitle))
        {
            return new OrderAssistantResult(
                SuggestedBudget: 0,
                SuggestedDuration: 1,
                RecommendedFreelancers: new List<string>(),
                Confidence: 0.0f,
                Reason: "Недостаточно данных для анализа.");
        }

        var payload = new
        {
            type = "orderAssistant",
            request
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var resultJson = await _rustBridge.RunInferenceAsync(json);

        if (string.IsNullOrWhiteSpace(resultJson))
        {
            return new OrderAssistantResult(
                SuggestedBudget: request.BudgetMin,
                SuggestedDuration: request.DurationDays,
                RecommendedFreelancers: new List<string>(),
                Confidence: 0.0f,
                Reason: "Ошибка инференса.");
        }

        try
        {
            var result = JsonSerializer.Deserialize<OrderAssistantResult>(resultJson, _jsonOptions);
            return result ?? new OrderAssistantResult(
                SuggestedBudget: request.BudgetMin,
                SuggestedDuration: request.DurationDays,
                RecommendedFreelancers: new List<string>(),
                Confidence: 0.0f,
                Reason: "Ошибка разбора результата.");
        }
        catch (JsonException)
        {
            return new OrderAssistantResult(
                SuggestedBudget: request.BudgetMin,
                SuggestedDuration: request.DurationDays,
                RecommendedFreelancers: new List<string>(),
                Confidence: 0.0f,
                Reason: "Ошибка десериализации.");
        }
    }
}