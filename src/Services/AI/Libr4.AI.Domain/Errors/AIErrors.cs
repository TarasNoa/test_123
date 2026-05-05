using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;

namespace Libr4.AI.Domain.Errors;

public static class AIErrors
{
    public static Error ChatNotFound => Error.NotFound(
        "AI.ChatNotFound",
        "Чат не найден");

    public static Error AgentNotFound => Error.NotFound(
        "AI.AgentNotFound",
        "Агент не найден");

    public static Error ProviderNotAvailable => Error.Failure(
        "AI.ProviderNotAvailable",
        "AI провайдер недоступен");

    public static Error ModelNotFound => Error.NotFound(
        "AI.ModelNotFound",
        "Модель не найдена");

    public static Error RateLimitExceeded => Error.Failure(
        "AI.RateLimitExceeded",
        "Превышен лимит запросов к API");

    public static Error InvalidAPIKey => Error.Unauthorized(
        "AI.InvalidAPIKey",
        "Неверный API ключ");

    public static Error GenerationFailed => Error.Failure(
        "AI.GenerationFailed",
        "Ошибка генерации ответа");

    public static Error ContextTooLarge => Error.Validation(
        "AI.ContextTooLarge",
        "Контекст превышает лимит токенов");

    public static Error ToolExecutionFailed => Error.Failure(
        "AI.ToolExecutionFailed",
        "Ошибка выполнения инструмента");

    public static Error UnauthorizedAccess => Error.Unauthorized(
        "AI.UnauthorizedAccess",
        "Нет доступа к чату");

    public static Error Unauthorized => Error.Unauthorized(
        "AI.Unauthorized",
        "Требуется аутентификация");
}
