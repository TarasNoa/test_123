using System.Text.RegularExpressions;

namespace Libr4.IDE.Application.Security;

public interface ICodeValidator
{
    ValidationResult Validate(string code, string language);
}

public record ValidationResult(bool IsValid, string? ErrorMessage = null);

public class CodeGuardian : ICodeValidator
{
    private const int MaxCodeLength = 50_000; // 50KB предел
    
    // Список потенциально опасных паттернов для Rust/C#
    private static readonly string[] BannedPatterns = 
    { 
        "std::fs", "std::net", "std::process", "Command::new", 
        "unsafe {", "libc::", "/etc/passwd", "winapi" 
    };

    public ValidationResult Validate(string code, string language)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new ValidationResult(false, "Код не может быть пустым.");

        if (code.Length > MaxCodeLength)
            return new ValidationResult(false, $"Код слишком велик (макс. {MaxCodeLength} символов).");

        // Проверка на запрещенные библиотеки/вызовы
        foreach (var pattern in BannedPatterns)
        {
            if (code.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(false, $"Обнаружен запрещенный вызов: {pattern}. Использование системных библиотек ограничено.");
            }
        }

        // Базовая проверка структуры (например, наличие main)
        if (language.ToLower() == "rust" && !code.Contains("fn main()"))
        {
            return new ValidationResult(false, "Отсутствует точка входа: fn main().");
        }

        return new ValidationResult(true);
    }
}
