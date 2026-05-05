namespace Libr4.Shared.Contracts.Mcp;

/// <summary>
/// Security layer for MCP adapters.
/// Provides PHI/PII protection, rate limiting, and access control.
/// </summary>
public interface IMcpSecurityLayer
{
    /// <summary>
    /// Checks if a query is allowed based on security policies.
    /// </summary>
    /// <param name="adapterId">ID of the adapter.</param>
    /// <param name="query">Query to check.</param>
    /// <param name="userId">ID of the user making the query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the query is allowed, false otherwise.</returns>
    Task<bool> IsQueryAllowedAsync(
        string adapterId,
        McpQuery query,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sanitizes query results to remove PHI/PII data.
    /// </summary>
    /// <param name="result">Query result to sanitize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sanitized query result.</returns>
    Task<McpQueryResult> SanitizeResultAsync(
        McpQueryResult result,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the rate limit has been exceeded for the adapter.
    /// </summary>
    /// <param name="adapterId">ID of the adapter.</param>
    /// <param name="userId">ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rate limit exceeded, false otherwise.</returns>
    Task<bool> IsRateLimitExceededAsync(
        string adapterId,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a query for rate limiting purposes.
    /// </summary>
    /// <param name="adapterId">ID of the adapter.</param>
    /// <param name="userId">ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RecordQueryAsync(
        string adapterId,
        string? userId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Policy for MCP datasource usage.
/// Defines which datasources can be used for which tasks.
/// </summary>
public interface IMcpUsagePolicy
{
    /// <summary>
    /// Checks if a datasource type is allowed for a specific task.
    /// </summary>
    /// <param name="datasourceType">Type of datasource.</param>
    /// <param name="taskType">Type of task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the datasource is allowed for the task, false otherwise.</returns>
    Task<bool> IsDatasourceAllowedForTaskAsync(
        string datasourceType,
        string taskType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets allowed datasources for a specific task.
    /// </summary>
    /// <param name="taskType">Type of task.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of allowed datasource types.</returns>
    Task<IReadOnlyList<string>> GetAllowedDatasourcesForTaskAsync(
        string taskType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a policy rule.
    /// </summary>
    /// <param name="datasourceType">Type of datasource.</param>
    /// <param name="taskType">Type of task.</param>
    /// <param name="allowed">Whether the datasource is allowed for the task.</param>
    void AddPolicyRule(string datasourceType, string taskType, bool allowed);

    /// <summary>
    /// Removes a policy rule.
    /// </summary>
    /// <param name="datasourceType">Type of datasource.</param>
    /// <param name="taskType">Type of task.</param>
    void RemovePolicyRule(string datasourceType, string taskType);
}

/// <summary>
/// In-memory implementation of MCP security layer.
/// </summary>
public class InMemoryMcpSecurityLayer : IMcpSecurityLayer
{
    private readonly Dictionary<string, List<DateTime>> _queryHistory = new();
    private readonly int _maxQueriesPerMinute = 60;

    public Task<bool> IsQueryAllowedAsync(
        string adapterId,
        McpQuery query,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        // Check rate limit
        var key = $"{adapterId}:{userId ?? "anonymous"}";
        if (_queryHistory.TryGetValue(key, out var history))
        {
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var recentQueries = history.Count(t => t > oneMinuteAgo);
            if (recentQueries >= _maxQueriesPerMinute)
            {
                return Task.FromResult(false);
            }
        }

        // Check for sensitive data patterns in query
        var queryText = query.QueryString.ToLowerInvariant();
        var hasSensitiveData = ContainsSensitiveData(queryText);
        
        return Task.FromResult(!hasSensitiveData);
    }

    public Task<McpQueryResult> SanitizeResultAsync(
        McpQueryResult result,
        CancellationToken cancellationToken = default)
    {
        var sanitizedResources = new List<McpResource>();
        
        foreach (var resource in result.Resources)
        {
            var sanitizedContent = SanitizeText(resource.Content ?? string.Empty);
            var sanitizedMetadata = new Dictionary<string, string>();
            
            foreach (var kvp in resource.Metadata)
            {
                sanitizedMetadata[kvp.Key] = SanitizeText(kvp.Value);
            }

            sanitizedResources.Add(resource with
            {
                Content = sanitizedContent,
                Metadata = sanitizedMetadata
            });
        }

        var sanitizedResult = result with { Resources = sanitizedResources };
        return Task.FromResult(sanitizedResult);
    }

    public Task<bool> IsRateLimitExceededAsync(
        string adapterId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{adapterId}:{userId ?? "anonymous"}";
        if (!_queryHistory.TryGetValue(key, out var history))
        {
            return Task.FromResult(false);
        }

        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        var recentQueries = history.Count(t => t > oneMinuteAgo);
        return Task.FromResult(recentQueries >= _maxQueriesPerMinute);
    }

    public Task RecordQueryAsync(
        string adapterId,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{adapterId}:{userId ?? "anonymous"}";
        
        if (!_queryHistory.ContainsKey(key))
        {
            _queryHistory[key] = new List<DateTime>();
        }

        _queryHistory[key].Add(DateTime.UtcNow);

        // Clean up old entries (older than 5 minutes)
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        _queryHistory[key] = _queryHistory[key]
            .Where(t => t > fiveMinutesAgo)
            .ToList();

        return Task.CompletedTask;
    }

    private static bool ContainsSensitiveData(string text)
    {
        // Check for common PHI/PII patterns
        var patterns = new[]
        {
            @"\b\d{3}-\d{2}-\d{4}\b", // SSN
            @"\b\d{16}\b", // Credit card
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", // Email
            @"\b\d{10}\b", // Phone number (US)
            @"password|secret|token|api_key", // Sensitive keywords
            @"ssn|social security|credit card|bank account" // PII keywords
        };

        return patterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(text, pattern, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Mask common sensitive patterns
        var sanitized = System.Text.RegularExpressions.Regex.Replace(
            text, @"\b\d{3}-\d{2}-\d{4}\b", "***-**-****");
        
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"\b\d{16}\b", "****************");
        
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "***@***.***");

        return sanitized;
    }
}

/// <summary>
/// In-memory implementation of MCP usage policy.
/// </summary>
public class InMemoryMcpUsagePolicy : IMcpUsagePolicy
{
    private readonly Dictionary<string, Dictionary<string, bool>> _policies = new();

    public Task<bool> IsDatasourceAllowedForTaskAsync(
        string datasourceType,
        string taskType,
        CancellationToken cancellationToken = default)
    {
        var key = datasourceType.ToLowerInvariant();
        
        if (!_policies.TryGetValue(key, out var taskPolicies))
        {
            // Default: allow all datasources for all tasks
            return Task.FromResult(true);
        }

        var taskKey = taskType.ToLowerInvariant();
        if (!taskPolicies.TryGetValue(taskKey, out var allowed))
        {
            // Default: allow if no specific rule exists
            return Task.FromResult(true);
        }

        return Task.FromResult(allowed);
    }

    public Task<IReadOnlyList<string>> GetAllowedDatasourcesForTaskAsync(
        string taskType,
        CancellationToken cancellationToken = default)
    {
        var allowed = new List<string>();
        var taskKey = taskType.ToLowerInvariant();

        foreach (var kvp in _policies)
        {
            if (kvp.Value.TryGetValue(taskKey, out var isAllowed) && isAllowed)
            {
                allowed.Add(kvp.Key);
            }
            else if (!kvp.Value.ContainsKey(taskKey))
            {
                // Default: allow if no specific rule exists
                allowed.Add(kvp.Key);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(allowed.AsReadOnly());
    }

    public void AddPolicyRule(string datasourceType, string taskType, bool allowed)
    {
        var key = datasourceType.ToLowerInvariant();
        var taskKey = taskType.ToLowerInvariant();

        if (!_policies.ContainsKey(key))
        {
            _policies[key] = new Dictionary<string, bool>();
        }

        _policies[key][taskKey] = allowed;
    }

    public void RemovePolicyRule(string datasourceType, string taskType)
    {
        var key = datasourceType.ToLowerInvariant();
        var taskKey = taskType.ToLowerInvariant();

        if (_policies.TryGetValue(key, out var taskPolicies))
        {
            taskPolicies.Remove(taskKey);
        }
    }
}
