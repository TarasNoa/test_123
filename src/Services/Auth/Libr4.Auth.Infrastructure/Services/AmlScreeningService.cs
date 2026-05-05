using Libr4.Auth.Application.Abstractions;
using Libr4.Auth.Domain.Kyc;

namespace Libr4.Auth.Infrastructure.Services;

public sealed class AmlScreeningService : IAmlScreeningService
{
    private readonly HttpClient _httpClient;
    private readonly string _provider;
    private readonly string _apiKey;

    public AmlScreeningService(HttpClient httpClient, string provider, string apiKey)
    {
        _httpClient = httpClient;
        _provider = provider;
        _apiKey = apiKey;
    }

    public async Task<AmlScreeningResult> ScreenAsync(string fullName, DateOnly dateOfBirth, string nationality, string country, CancellationToken ct)
    {
        return _provider.ToLower() switch
        {
            "sumsub" => await ScreenWithSumsubAsync(fullName, dateOfBirth, nationality, country, ct),
            "persona" => await ScreenWithPersonaAsync(fullName, dateOfBirth, nationality, country, ct),
            _ => throw new InvalidOperationException($"Unknown AML provider: {_provider}")
        };
    }

    private async Task<AmlScreeningResult> ScreenWithSumsubAsync(string fullName, DateOnly dateOfBirth, string nationality, string country, CancellationToken ct)
    {
        var request = new
        {
            firstName = fullName.Split(' ')[0],
            lastName = string.Join(" ", fullName.Split(' ').Skip(1)),
            dob = dateOfBirth.ToString("yyyy-MM-dd"),
            nationality = nationality,
            country = country
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.sumsub.com/resources/applicants/-/one-step-screenings")
        {
            Content = content
        };
        httpRequest.Headers.Add("X-App-Token", _apiKey);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new AmlScreeningResult(false, false, RiskRating.Low, "Screening failed", null);
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            var isPep = root.TryGetProperty("isPep", out var pepProp) && pepProp.GetBoolean();
            var sanctionsHit = root.TryGetProperty("sanctionsHit", out var sanctionsProp) && sanctionsProp.GetBoolean();
            var riskRating = ParseRiskRating(root);
            var refId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            return new AmlScreeningResult(isPep, sanctionsHit, riskRating, "Sumsub screening completed", refId);
        }
        catch (Exception ex)
        {
            return new AmlScreeningResult(false, false, RiskRating.Low, $"Screening error: {ex.Message}", null);
        }
    }

    private async Task<AmlScreeningResult> ScreenWithPersonaAsync(string fullName, DateOnly dateOfBirth, string nationality, string country, CancellationToken ct)
    {
        var request = new
        {
            name = fullName,
            birthDate = dateOfBirth.ToString("yyyy-MM-dd"),
            nationality = nationality,
            country = country
        };

        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.withpersona.com/api/v1/screenings")
        {
            Content = content
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new AmlScreeningResult(false, false, RiskRating.Low, "Screening failed", null);
            }

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;

            var isPep = root.TryGetProperty("isPep", out var pepProp) && pepProp.GetBoolean();
            var sanctionsHit = root.TryGetProperty("sanctionsHit", out var sanctionsProp) && sanctionsProp.GetBoolean();
            var riskRating = ParseRiskRating(root);
            var refId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

            return new AmlScreeningResult(isPep, sanctionsHit, riskRating, "Persona screening completed", refId);
        }
        catch (Exception ex)
        {
            return new AmlScreeningResult(false, false, RiskRating.Low, $"Screening error: {ex.Message}", null);
        }
    }

    private static RiskRating ParseRiskRating(System.Text.Json.JsonElement root)
    {
        if (root.TryGetProperty("riskRating", out var ratingProp))
        {
            var rating = ratingProp.GetString()?.ToLower();
            return rating switch
            {
                "high" => RiskRating.High,
                "medium" => RiskRating.Medium,
                "low" => RiskRating.Low,
                _ => RiskRating.Low
            };
        }
        return RiskRating.Low;
    }
}
