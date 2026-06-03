using Libr4.IDE.Application.AutonomousAppGeneration.Commands;
using Libr4.IDE.Application.AutonomousAppGeneration.Infrastructure;
using Libr4.IDE.Application.AutonomousAppGeneration.Services;

namespace Libr4.IDE.Api;

public interface IAppGenerationChatBridge
{
    bool ShouldStartGeneration(string message);

    Task<ChatAppGenerationStartResult> TryStartFromChatAsync(
        string message,
        string sessionId,
        string? tenantId,
        CancellationToken ct = default);
}

public sealed record ChatAppGenerationStartResult(
    Guid RunId,
    string Status,
    string AssistantMessage,
    string ReportUrl);

public sealed class AppGenerationChatBridge : IAppGenerationChatBridge
{
    private readonly IAppGenerationRunStarter _starter;

    public AppGenerationChatBridge(IAppGenerationRunStarter starter) => _starter = starter;

    public bool ShouldStartGeneration(string message) =>
        AppGenerationChatIntentDetector.IsAppGenerationRequest(message);

    public async Task<ChatAppGenerationStartResult> TryStartFromChatAsync(
        string message,
        string sessionId,
        string? tenantId,
        CancellationToken ct = default)
    {
        var command = new StartAppGenerationCommand(
            UserRequest: message.Trim(),
            MaxIterations: 20,
            TriggerSource: "ide_chat",
            TriggerActor: sessionId,
            TenantId: tenantId);

        var started = await _starter.StartInBackgroundAsync(command, ct).ConfigureAwait(false);
        if (started.RunId is null)
            throw new InvalidOperationException(started.Message);

        var reportUrl = $"/api/v1/ide/app-generation/{started.RunId:D}";
        var assistantMessage =
            "Запустил автономную генерацию приложения по вашему запросу.\n\n" +
            $"**Run ID:** `{started.RunId:D}`\n" +
            $"**Статус:** {started.Status}\n\n" +
            "Прогресс и файлы появятся в отчёте генерации. Я буду обновлять статус в этом чате.";

        return new ChatAppGenerationStartResult(
            started.RunId.Value,
            started.Status,
            assistantMessage,
            reportUrl);
    }
}
