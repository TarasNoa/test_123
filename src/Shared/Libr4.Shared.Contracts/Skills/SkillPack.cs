namespace Libr4.Shared.Contracts.Skills;

/// <summary>
/// Skill pack - a collection of domain-specific agent skills with contracts and governance.
/// </summary>
public record SkillPack
{
    /// <summary>
    /// Unique identifier for the skill pack.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the skill pack.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the skill pack provides.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Domain the skill pack belongs to (e.g., "web_development", "data_science", "devops").
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// Version of the skill pack.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Author of the skill pack.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Skills included in the pack.
    /// </summary>
    public List<SkillDefinition> Skills { get; init; } = new();

    /// <summary>
    /// Contracts for the skills.
    /// </summary>
    public List<SkillContract> Contracts { get; init; } = new();

    /// <summary>
    /// Governance metadata.
    /// </summary>
    public SkillGovernance Governance { get; init; } = new();

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// When the skill pack was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the skill pack was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>
    /// Status of the skill pack.
    /// </summary>
    public SkillPackStatus Status { get; init; } = SkillPackStatus.Draft;
}

/// <summary>
/// Status of a skill pack.
/// </summary>
public enum SkillPackStatus
{
    Draft,
    PendingApproval,
    Approved,
    Deprecated,
    Retired
}

/// <summary>
/// Definition of a single skill.
/// </summary>
public record SkillDefinition
{
    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the skill.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description of what the skill does.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Type of skill (e.g., "code_generation", "testing", "deployment").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Input schema for the skill.
    /// </summary>
    public string? InputSchema { get; init; }

    /// <summary>
    /// Output schema for the skill.
    /// </summary>
    public string? OutputSchema { get; init; }

    /// <summary>
    /// Capabilities of the skill.
    /// </summary>
    public List<string> Capabilities { get; init; } = new();

    /// <summary>
    /// Dependencies on other skills.
    /// </summary>
    public List<string> Dependencies { get; init; } = new();
}

/// <summary>
/// Contract for a skill.
/// </summary>
public record SkillContract
{
    /// <summary>
    /// Unique identifier for the contract.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Skill ID this contract applies to.
    /// </summary>
    public string SkillId { get; init; } = string.Empty;

    /// <summary>
    /// Contract type (e.g., "input_validation", "output_validation", "performance_sla").
    /// </summary>
    public string ContractType { get; init; } = string.Empty;

    /// <summary>
    /// Contract rules or constraints.
    /// </summary>
    public string Rules { get; init; } = string.Empty;

    /// <summary>
    /// Whether the contract is enforced.
    /// </summary>
    public bool Enforced { get; init; } = true;
}

/// <summary>
/// Governance metadata for a skill pack.
/// </summary>
public record SkillGovernance
{
    /// <summary>
    /// Approval status.
    /// </summary>
    public ApprovalStatus ApprovalStatus { get; init; } = ApprovalStatus.Pending;

    /// <summary>
    /// List of approvers.
    /// </summary>
    public List<string> Approvers { get; init; } = new();

    /// <summary>
    /// Approval timestamp.
    /// </summary>
    public DateTime? ApprovedAt { get; init; }

    /// <summary>
    /// Approver comments.
    /// </summary>
    public string? ApprovalComments { get; init; }

    /// <summary>
    /// Security classification.
    /// </summary>
    public SecurityClassification SecurityClassification { get; init; } = SecurityClassification.Public;

    /// <summary>
    /// Allowed roles for using this skill pack.
    /// </summary>
    public List<string> AllowedRoles { get; init; } = new();

    /// <summary>
    /// Rate limit (requests per minute).
    /// </summary>
    public int? RateLimitPerMinute { get; init; }

    /// <summary>
    /// Whether the skill pack is deprecated.
    /// </summary>
    public bool IsDeprecated { get; init; }

    /// <summary>
    /// Deprecation reason.
    /// </summary>
    public string? DeprecationReason { get; init; }

    /// <summary>
    /// Deprecation date.
    /// </summary>
    public DateTime? DeprecatedAt { get; init; }
}

/// <summary>
/// Approval status.
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// Security classification.
/// </summary>
public enum SecurityClassification
{
    Public,
    Internal,
    Confidential,
    Restricted
}

/// <summary>
/// Repository for managing skill packs.
/// </summary>
public interface ISkillPackRepository
{
    /// <summary>
    /// Gets a skill pack by ID.
    /// </summary>
    /// <param name="id">ID of the skill pack.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The skill pack, or null if not found.</returns>
    Task<SkillPack?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all skill packs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of all skill packs.</returns>
    Task<IReadOnlyList<SkillPack>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets skill packs by domain.
    /// </summary>
    /// <param name="domain">Domain to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of skill packs in the domain.</returns>
    Task<IReadOnlyList<SkillPack>> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approved skill packs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of approved skill packs.</returns>
    Task<IReadOnlyList<SkillPack>> GetApprovedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a skill pack.
    /// </summary>
    /// <param name="skillPack">Skill pack to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved skill pack.</returns>
    Task<SkillPack> SaveAsync(SkillPack skillPack, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a skill pack.
    /// </summary>
    /// <param name="id">ID of the skill pack to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a skill pack for approval.
    /// </summary>
    /// <param name="id">ID of the skill pack.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated skill pack.</returns>
    Task<SkillPack> SubmitForApprovalAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a skill pack.
    /// </summary>
    /// <param name="id">ID of the skill pack.</param>
    /// <param name="approver">Approver ID.</param>
    /// <param name="comments">Approval comments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated skill pack.</returns>
    Task<SkillPack> ApproveAsync(string id, string approver, string? comments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a skill pack.
    /// </summary>
    /// <param name="id">ID of the skill pack.</param>
    /// <param name="approver">Approver ID.</param>
    /// <param name="reason">Rejection reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated skill pack.</returns>
    Task<SkillPack> RejectAsync(string id, string approver, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deprecates a skill pack.
    /// </summary>
    /// <param name="id">ID of the skill pack.</param>
    /// <param name="reason">Deprecation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated skill pack.</returns>
    Task<SkillPack> DeprecateAsync(string id, string reason, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of skill pack repository.
/// </summary>
public class InMemorySkillPackRepository : ISkillPackRepository
{
    private readonly Dictionary<string, SkillPack> _skillPacks = new();

    public Task<SkillPack?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _skillPacks.TryGetValue(id, out var skillPack);
        return Task.FromResult(skillPack);
    }

    public Task<IReadOnlyList<SkillPack>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SkillPack>>(_skillPacks.Values.ToList().AsReadOnly());
    }

    public Task<IReadOnlyList<SkillPack>> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        var skillPacks = _skillPacks.Values
            .Where(sp => sp.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<SkillPack>>(skillPacks);
    }

    public Task<IReadOnlyList<SkillPack>> GetApprovedAsync(CancellationToken cancellationToken = default)
    {
        var skillPacks = _skillPacks.Values
            .Where(sp => sp.Governance.ApprovalStatus == ApprovalStatus.Approved && sp.Status == SkillPackStatus.Approved)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<SkillPack>>(skillPacks);
    }

    public Task<SkillPack> SaveAsync(SkillPack skillPack, CancellationToken cancellationToken = default)
    {
        var saved = skillPack with { UpdatedAt = DateTime.UtcNow };
        _skillPacks[saved.Id] = saved;
        return Task.FromResult(saved);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_skillPacks.Remove(id));
    }

    public Task<SkillPack> SubmitForApprovalAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!_skillPacks.TryGetValue(id, out var skillPack))
        {
            throw new ArgumentException($"Skill pack with ID {id} not found", nameof(id));
        }

        var updated = skillPack with
        {
            Status = SkillPackStatus.PendingApproval,
            UpdatedAt = DateTime.UtcNow,
            Governance = skillPack.Governance with { ApprovalStatus = ApprovalStatus.Pending }
        };

        _skillPacks[id] = updated;
        return Task.FromResult(updated);
    }

    public Task<SkillPack> ApproveAsync(string id, string approver, string? comments = null, CancellationToken cancellationToken = default)
    {
        if (!_skillPacks.TryGetValue(id, out var skillPack))
        {
            throw new ArgumentException($"Skill pack with ID {id} not found", nameof(id));
        }

        var updated = skillPack with
        {
            Status = SkillPackStatus.Approved,
            UpdatedAt = DateTime.UtcNow,
            Governance = skillPack.Governance with
            {
                ApprovalStatus = ApprovalStatus.Approved,
                Approvers = skillPack.Governance.Approvers.Concat(new[] { approver }).ToList(),
                ApprovedAt = DateTime.UtcNow,
                ApprovalComments = comments
            }
        };

        _skillPacks[id] = updated;
        return Task.FromResult(updated);
    }

    public Task<SkillPack> RejectAsync(string id, string approver, string reason, CancellationToken cancellationToken = default)
    {
        if (!_skillPacks.TryGetValue(id, out var skillPack))
        {
            throw new ArgumentException($"Skill pack with ID {id} not found", nameof(id));
        }

        var updated = skillPack with
        {
            Status = SkillPackStatus.Draft,
            UpdatedAt = DateTime.UtcNow,
            Governance = skillPack.Governance with
            {
                ApprovalStatus = ApprovalStatus.Rejected,
                ApprovalComments = $"Rejected by {approver}: {reason}"
            }
        };

        _skillPacks[id] = updated;
        return Task.FromResult(updated);
    }

    public Task<SkillPack> DeprecateAsync(string id, string reason, CancellationToken cancellationToken = default)
    {
        if (!_skillPacks.TryGetValue(id, out var skillPack))
        {
            throw new ArgumentException($"Skill pack with ID {id} not found", nameof(id));
        }

        var updated = skillPack with
        {
            Status = SkillPackStatus.Deprecated,
            UpdatedAt = DateTime.UtcNow,
            Governance = skillPack.Governance with
            {
                IsDeprecated = true,
                DeprecationReason = reason,
                DeprecatedAt = DateTime.UtcNow
            }
        };

        _skillPacks[id] = updated;
        return Task.FromResult(updated);
    }
}
