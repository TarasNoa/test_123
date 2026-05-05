using Libr4.Shared.Kernel.Domain;

namespace Libr4.Integrations.Domain.ExternalApis;

public enum ApiType
{
    CryptoPrices,
    ExchangeRates,
    Geolocation,
    Weather,
    Custom
}

public enum ApiStatus
{
    Active,
    Inactive,
    Error,
    RateLimited
}

public class ExternalApi : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public ApiType Type { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public ApiStatus Status { get; private set; }
    public string? ApiKey { get; private set; }
    public int RateLimit { get; private set; } // requests per minute
    public int CurrentUsage { get; private set; }
    public DateTime LastCallAt { get; private set; }
    public DateTime? RateLimitResetAt { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<ApiCall> _callHistory = new();
    public IReadOnlyCollection<ApiCall> CallHistory => _callHistory.AsReadOnly();

    private ExternalApi() { }

    public static ExternalApi Create(string name, ApiType type, string endpoint, int rateLimit = 60, string? apiKey = null)
    {
        return new ExternalApi
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Endpoint = endpoint,
            Status = ApiStatus.Active,
            ApiKey = apiKey,
            RateLimit = rateLimit,
            CurrentUsage = 0,
            LastCallAt = DateTime.UtcNow,
            SuccessCount = 0,
            FailureCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void RecordCall(bool success, string? errorMessage = null)
    {
        var call = ApiCall.Create(Id, success, errorMessage);
        _callHistory.Add(call);

        if (success)
        {
            SuccessCount++;
            Status = ApiStatus.Active;
        }
        else
        {
            FailureCount++;
            Status = ApiStatus.Error;
        }

        CurrentUsage++;
        LastCallAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Check if rate limit exceeded
        if (CurrentUsage >= RateLimit)
        {
            Status = ApiStatus.RateLimited;
            RateLimitResetAt = DateTime.UtcNow.AddMinutes(1);
        }
    }

    public void ResetRateLimit()
    {
        CurrentUsage = 0;
        RateLimitResetAt = null;
        Status = ApiStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Status = ApiStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = ApiStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public float SuccessRate => (SuccessCount + FailureCount) > 0 ? (float)SuccessCount / (SuccessCount + FailureCount) * 100 : 0;
}

public class ApiCall : Entity<Guid>
{
    public Guid ApiId { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int ResponseTimeMs { get; private set; }
    public DateTime CalledAt { get; private set; }

    private ApiCall() { }

    public static ApiCall Create(Guid apiId, bool success, string? errorMessage = null)
    {
        return new ApiCall
        {
            Id = Guid.NewGuid(),
            ApiId = apiId,
            Success = success,
            ErrorMessage = errorMessage,
            ResponseTimeMs = 0, // Would be set by the actual call
            CalledAt = DateTime.UtcNow
        };
    }

    public void SetResponseTime(int ms)
    {
        ResponseTimeMs = ms;
    }
}
