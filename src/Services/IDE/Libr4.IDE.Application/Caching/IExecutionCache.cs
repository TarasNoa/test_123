using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Libr4.IDE.Application.Caching;

public record CachedResult(string Stdout, string Stderr, string TerminationReason);

public interface IExecutionCache
{
    Task<CachedResult?> GetAsync(string code, string language);
    Task SetAsync(string code, string language, CachedResult result);
}

public class ExecutionCache : IExecutionCache
{
    // В реальном проде здесь должен быть Redis (IDistributedCache)
    // Для текущего стека используем MemoryCache как эффективную прослойку
    private readonly Dictionary<string, (CachedResult Result, DateTime Expiry)> _cache = new();
    private readonly TimeSpan _defaultTtl = TimeSpan.FromHours(1);

    public async Task<CachedResult?> GetAsync(string code, string language)
    {
        var key = GenerateKey(code, language);
        if (_cache.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
        {
            return entry.Result;
        }
        return null;
    }

    public async Task SetAsync(string code, string language, CachedResult result)
    {
        var key = GenerateKey(code, language);
        _cache[key] = (result, DateTime.UtcNow.Add(_defaultTtl));
    }

    private string GenerateKey(string code, string language)
    {
        // Хэшируем код и язык для создания уникального ключа
        var input = $"{language}:{code}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
