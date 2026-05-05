/*
using Libr4.Gateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Libr4.IDE.Application.FreelancerMarketplace;

/// <summary>
/// Integration service between EscrowCodeService and Gateway's DynamicPreviewRouter
/// Automatically registers preview routes when escrow workspaces are created
/// </summary>
public interface IGatewayPreviewIntegration
{
    Task<string> RegisterPreviewAsync(string orderId, string customerId, string containerId);
    Task UnregisterPreviewAsync(string orderId);
    Task ExtendPreviewExpiryAsync(string orderId, TimeSpan extension);
}

public class GatewayPreviewIntegration : IGatewayPreviewIntegration
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GatewayPreviewIntegration> _logger;
    private readonly string _gatewayBaseUrl;

    public GatewayPreviewIntegration(
        HttpClient httpClient,
        ILogger<GatewayPreviewIntegration> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _gatewayBaseUrl = configuration["Gateway:BaseUrl"] ?? "http://localhost:5000";
    }

    /// <summary>
    /// Register a preview route with the Gateway
    /// </summary>
    public async Task<string> RegisterPreviewAsync(
        string orderId,
        string customerId,
        string containerId)
    {
        try
        {
            // Get container endpoint from container manager or Docker
            var containerEndpoint = await GetContainerEndpointAsync(containerId);

            var request = new
            {
                OrderId = orderId,
                CustomerId = customerId,
                ContainerEndpoint = containerEndpoint,
                ContainerPort = 3000
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_gatewayBaseUrl}/api/gateway/previews/register",
                request);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PreviewRegistrationResponse>();
            var previewUrl = result?.PreviewUrl ?? $"/preview/{HashCustomerId(customerId)}/{orderId}";

            _logger.LogInformation(
                "Registered preview for order {OrderId} at {PreviewUrl}",
                orderId, previewUrl);

            return previewUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register preview for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Unregister a preview route from the Gateway
    /// </summary>
    public async Task UnregisterPreviewAsync(string orderId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"{_gatewayBaseUrl}/api/gateway/previews/{orderId}");

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Unregistered preview for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister preview for order {OrderId}", orderId);
            throw;
        }
    }

    /// <summary>
    /// Extend preview expiry time via Gateway API
    /// </summary>
    public async Task ExtendPreviewExpiryAsync(string orderId, TimeSpan extension)
    {
        try
        {
            var request = new
            {
                OrderId = orderId,
                ExtensionMinutes = (int)extension.TotalMinutes
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_gatewayBaseUrl}/api/gateway/previews/{orderId}/extend",
                request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Extended preview expiry for order {OrderId} by {Extension}",
                    orderId, extension);
            }
            else
            {
                // Gateway may not support extension yet - log warning
                _logger.LogWarning(
                    "Gateway preview extension not available for order {OrderId}. " +
                    "Status: {StatusCode}. Consider implementing in Gateway.",
                    orderId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend preview for order {OrderId}", orderId);
            // Don't throw - extension is non-critical
        }
    }

    /// <summary>
    /// Get the container's network endpoint (IP or hostname)
    /// </summary>
    private async Task<string> GetContainerEndpointAsync(string containerId)
    {
        // In a Docker environment, we need to get the container's IP address
        // or use the container name as DNS if using Docker Compose networking

        // Simplified: use container ID as hostname (works with Docker Compose)
        return containerId;

        // Alternative: Get actual IP from Docker API
        // This would require Docker API access
    }

    private string HashCustomerId(string customerId)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(customerId));
        return Convert.ToHexString(bytes)[..16].ToLower();
    }
}

/// <summary>
/// Response from Gateway preview registration
/// </summary>
public class PreviewRegistrationResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
*/
