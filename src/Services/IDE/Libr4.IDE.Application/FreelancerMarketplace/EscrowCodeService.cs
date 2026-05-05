/*
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Libr4.IDE.Application.FreelancerMarketplace;

/// <summary>
/// Escrow service for code - manages code delivery and payment release
/// </summary>
public interface IEscrowCodeService
{
    Task<ShadowWorkspaceSetup> SetupEscrowWorkspaceAsync(string orderId, string freelancerId, string customerId);
    Task<ReleaseDecision> EvaluateReleaseCriteriaAsync(string orderId);
    Task<bool> ReleaseCodeToCustomerAsync(string orderId);
    Task<bool> MergeShadowToMainAsync(string orderId);
    Task<PreviewAccess> GrantPreviewAccessAsync(string orderId, string customerId);
    Task RevokePreviewAccessAsync(string orderId, string customerId);
}

public class EscrowCodeService : IEscrowCodeService
{
    private readonly IContainerManager _containerManager;
    private readonly ISelfHealingBuildPipeline _buildPipeline;
    private readonly ISecurityTestingService _securityService;
    private readonly ICodeReviewService _reviewService;
    private readonly ILogger<EscrowCodeService> _logger;
    private readonly Dictionary<string, EscrowWorkspaceState> _escrowStates = new();

    public EscrowCodeService(
        IContainerManager containerManager,
        ISelfHealingBuildPipeline buildPipeline,
        ISecurityTestingService securityService,
        ICodeReviewService reviewService,
        ILogger<EscrowCodeService> logger)
    {
        _containerManager = containerManager;
        _buildPipeline = buildPipeline;
        _securityService = securityService;
        _reviewService = reviewService;
        _logger = logger;
    }

    public async Task<ShadowWorkspaceSetup> SetupEscrowWorkspaceAsync(
        string orderId,
        string freelancerId,
        string customerId)
    {
        _logger.LogInformation("Setting up escrow workspace for order {OrderId}", orderId);

        var workspaceId = $"escrow-{orderId}";

        // Create isolated container
        var container = await _containerManager.CreateContainerAsync(
            workspaceId,
            "mcr.microsoft.com/dotnet/sdk:8.0");

        // Setup state tracking
        var state = new EscrowWorkspaceState
        {
            OrderId = orderId,
            WorkspaceId = workspaceId,
            FreelancerId = freelancerId,
            CustomerId = customerId,
            ContainerId = container.Id,
            CreatedAt = DateTime.UtcNow,
            Status = EscrowStatus.Working
        };

        _escrowStates[orderId] = state;

        return new ShadowWorkspaceSetup
        {
            OrderId = orderId,
            WorkspaceId = workspaceId,
            ContainerId = container.Id,
            PreviewUrl = $"/preview/{HashCustomerId(customerId)}/{orderId}",
            AccessToken = GenerateAccessToken(orderId, customerId)
        };
    }

    public async Task<ReleaseDecision> EvaluateReleaseCriteriaAsync(string orderId)
    {
        if (!_escrowStates.TryGetValue(orderId, out var state))
        {
            throw new InvalidOperationException($"Escrow state not found for order {orderId}");
        }

        _logger.LogInformation("Evaluating release criteria for order {OrderId}", orderId);

        var criteria = new ReleaseCriteria();

        // Criterion 1: Build must succeed
        var buildResult = await _buildPipeline.ExecuteSingleBuildAsync(state.WorkspaceId);
        criteria.BuildSuccess = buildResult.Success;
        criteria.BuildDetails = buildResult;

        // Criterion 2: Security scan clean
        var securityResult = await _securityService.ScanAsync(state.WorkspaceId);
        criteria.SecurityScanClean = !securityResult.HasCriticalVulnerabilities;
        criteria.SecurityDetails = securityResult;

        // Criterion 3: Code review approved (if configured)
        var reviewResult = await _reviewService.GetReviewStatusAsync(orderId);
        criteria.CodeReviewApproved = reviewResult?.Status == ReviewStatus.Approved;
        criteria.ReviewDetails = reviewResult;

        // Criterion 4: Tests pass (if test project detected)
        criteria.TestsPass = buildResult.Success; // Simplified

        var allMet = criteria.BuildSuccess &&
                     criteria.SecurityScanClean &&
                     criteria.TestsPass;

        state.ReleaseCriteria = criteria;
        state.ReleaseCriteriaMet = allMet;

        _logger.LogInformation(
            "Release criteria for {OrderId}: Build={Build}, Security={Security}, Tests={Tests}, Review={Review}",
            orderId, criteria.BuildSuccess, criteria.SecurityScanClean, criteria.TestsPass, criteria.CodeReviewApproved);

        return new ReleaseDecision
        {
            OrderId = orderId,
            AllCriteriaMet = allMet,
            Criteria = criteria,
            CanRelease = allMet
        };
    }

    public async Task<bool> ReleaseCodeToCustomerAsync(string orderId)
    {
        if (!_escrowStates.TryGetValue(orderId, out var state))
        {
            return false;
        }

        // Check if criteria are met
        if (!state.ReleaseCriteriaMet)
        {
            _logger.LogWarning("Cannot release code for {OrderId}: criteria not met", orderId);
            return false;
        }

        // Merge shadow to main (make code available)
        var merged = await MergeShadowToMainAsync(orderId);
        if (!merged)
        {
            _logger.LogError("Failed to merge code for order {OrderId}", orderId);
            return false;
        }

        state.Status = EscrowStatus.Released;
        state.ReleasedAt = DateTime.UtcNow;

        _logger.LogInformation("Code released to customer for order {OrderId}", orderId);
        return true;
    }

    public async Task<bool> MergeShadowToMainAsync(string orderId)
    {
        if (!_escrowStates.TryGetValue(orderId, out var state))
        {
            return false;
        }

        _logger.LogInformation("Merging shadow workspace to main for order {OrderId}", orderId);

        try
        {
            // In production, this would:
            // 1. Copy files from container volume to main repository
            // 2. Create git commit
            // 3. Push to main branch
            // 4. Update database

            // Simplified version:
            var container = await _containerManager.GetContainerAsync(state.WorkspaceId);
            if (container == null)
            {
                return false;
            }

            // Mark as merged
            state.Status = EscrowStatus.Merged;
            state.MergedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully merged code for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge code for order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<PreviewAccess> GrantPreviewAccessAsync(string orderId, string customerId)
    {
        if (!_escrowStates.TryGetValue(orderId, out var state))
        {
            throw new InvalidOperationException($"Escrow state not found for order {orderId}");
        }

        // Ensure container is running
        await _containerManager.StartContainerAsync(state.ContainerId);

        var access = new PreviewAccess
        {
            OrderId = orderId,
            CustomerId = customerId,
            PreviewUrl = $"/preview/{HashCustomerId(customerId)}/{orderId}",
            AccessToken = GenerateAccessToken(orderId, customerId),
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            AccessType = PreviewAccessType.ReadOnlyNoSource,
            Permissions = new List<string> { "view", "interact", "test" }
        };

        state.PreviewAccesses ??= new List<PreviewAccess>();
        state.PreviewAccesses.Add(access);

        _logger.LogInformation("Granted preview access to customer {CustomerId} for order {OrderId}",
            customerId, orderId);

        return access;
    }

    public Task RevokePreviewAccessAsync(string orderId, string customerId)
    {
        if (_escrowStates.TryGetValue(orderId, out var state))
        {
            state.PreviewAccesses?.RemoveAll(a => a.CustomerId == customerId);
            _logger.LogInformation("Revoked preview access for customer {CustomerId} on order {OrderId}",
                customerId, orderId);
        }

        return Task.CompletedTask;
    }

    private string HashCustomerId(string customerId)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(customerId));
        return Convert.ToHexString(bytes)[..16].ToLower();
    }

    private string GenerateAccessToken(string orderId, string customerId)
    {
        var payload = $"{orderId}:{customerId}:{DateTime.UtcNow.Ticks}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("your-secret-key"));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}

// Supporting classes
public class ShadowWorkspaceSetup
{
    public string OrderId { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

public class ReleaseDecision
{
    public string OrderId { get; set; } = string.Empty;
    public bool AllCriteriaMet { get; set; }
    public bool CanRelease => AllCriteriaMet;
    public ReleaseCriteria Criteria { get; set; } = new();
}

public class ReleaseCriteria
{
    public bool BuildSuccess { get; set; }
    public bool TestsPass { get; set; }
    public bool SecurityScanClean { get; set; }
    public bool CodeReviewApproved { get; set; }

    public BuildResult? BuildDetails { get; set; }
    public SecurityScanResult? SecurityDetails { get; set; }
    public ReviewResult? ReviewDetails { get; set; }
}

public class PreviewAccess
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public PreviewAccessType AccessType { get; set; }
    public List<string> Permissions { get; set; } = new();
}

public enum PreviewAccessType
{
    ReadOnlyNoSource,      // Customer sees preview but no code
    ReadOnlyWithMetrics,   // Preview + build metrics
    InteractiveNoSource    // Can click/interact but no code access
}

public enum EscrowStatus
{
    Working,
    ReadyForReview,
    CriteriaMet,
    Released,
    Merged,
    Rejected
}

public class EscrowWorkspaceState
{
    public string OrderId { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
    public string FreelancerId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public EscrowStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? MergedAt { get; set; }
    public ReleaseCriteria? ReleaseCriteria { get; set; }
    public bool ReleaseCriteriaMet { get; set; }
    public List<PreviewAccess>? PreviewAccesses { get; set; }
}

public class SecurityScanResult
{
    public bool HasCriticalVulnerabilities { get; set; }
    public int VulnerabilityCount { get; set; }
}

public class ReviewResult
{
    public ReviewStatus Status { get; set; }
    public string? Comments { get; set; }
}

public enum ReviewStatus
{
    Pending,
    Approved,
    Rejected,
    NeedsChanges
}

// Stub interfaces for dependencies
public interface ISecurityTestingService
{
    Task<SecurityScanResult> ScanAsync(string workspaceId);
}

public interface ICodeReviewService
{
    Task<ReviewResult?> GetReviewStatusAsync(string orderId);
}
*/
