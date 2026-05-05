/*
namespace Libr4.IDE.Application.FreelancerMarketplace;

/// <summary>
/// Stub implementation for security testing service
/// </summary>
public class SecurityTestingServiceStub : ISecurityTestingService
{
    public Task<SecurityScanResult> ScanAsync(string workspaceId)
    {
        // Stub: Always returns clean
        return Task.FromResult(new SecurityScanResult
        {
            HasCriticalVulnerabilities = false,
            VulnerabilityCount = 0
        });
    }
}

/// <summary>
/// Stub implementation for code review service
/// </summary>
public class CodeReviewServiceStub : ICodeReviewService
{
    public Task<ReviewResult?> GetReviewStatusAsync(string orderId)
    {
        // Stub: Always returns approved
        return Task.FromResult<ReviewResult?>(new ReviewResult
        {
            Status = ReviewStatus.Approved
        });
    }
}
*/
