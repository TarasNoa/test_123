namespace Libr4.IDE.Application.Translation;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default);
    Task<string[]> TranslateBatchAsync(string[] texts, string targetLanguage, CancellationToken ct = default);
}

public record TranslationResult(string TranslatedText, string SourceLanguage, string TargetLanguage);
