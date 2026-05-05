using Libr4.Shared.Kernel.Domain;

namespace Libr4.CRM.Domain.Users;

public enum AccountStatus
{
    Active,
    Inactive,
    Suspended,
    Deleted
}

public class UserManagement : AggregateRoot<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public AccountStatus Status { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? SuspendedAt { get; private set; }
    public string? SuspensionReason { get; private set; }

    private readonly List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private readonly List<UserPermission> _permissions = new();
    public IReadOnlyCollection<UserPermission> Permissions => _permissions.AsReadOnly();

    private UserManagement() { }

    public static UserManagement Create(string email, string username)
    {
        return new UserManagement
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Status = AccountStatus.Active,
            IsVerified = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Verify()
    {
        IsVerified = true;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Suspend(string reason)
    {
        Status = AccountStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        SuspensionReason = reason;
    }

    public void Activate()
    {
        Status = AccountStatus.Active;
        SuspendedAt = null;
        SuspensionReason = null;
    }

    public void Deactivate()
    {
        Status = AccountStatus.Inactive;
    }

    public void AddRole(UserRole role)
    {
        _roles.Add(role);
    }

    public void RemoveRole(Guid roleId)
    {
        var role = _roles.FirstOrDefault(r => r.Id == roleId);
        if (role != null)
        {
            _roles.Remove(role);
        }
    }

    public void AddPermission(UserPermission permission)
    {
        _permissions.Add(permission);
    }

    public void RemovePermission(Guid permissionId)
    {
        var permission = _permissions.FirstOrDefault(p => p.Id == permissionId);
        if (permission != null)
        {
            _permissions.Remove(permission);
        }
    }

    public bool HasRole(string roleName)
    {
        return _roles.Any(r => r.Name == roleName);
    }

    public bool HasPermission(string permissionName)
    {
        return _permissions.Any(p => p.Name == permissionName);
    }
}

public class UserRole : Entity<Guid>
{
    public Guid UserManagementId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private UserRole() { }

    public static UserRole Create(Guid userManagementId, string name, string? description = null)
    {
        return new UserRole
        {
            Id = Guid.NewGuid(),
            UserManagementId = userManagementId,
            Name = name,
            Description = description,
            AssignedAt = DateTime.UtcNow
        };
    }
}

public class UserPermission : Entity<Guid>
{
    public Guid UserManagementId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Resource { get; private set; }
    public string? Action { get; private set; }
    public DateTime GrantedAt { get; private set; }

    private UserPermission() { }

    public static UserPermission Create(Guid userManagementId, string name, string? resource = null, string? action = null)
    {
        return new UserPermission
        {
            Id = Guid.NewGuid(),
            UserManagementId = userManagementId,
            Name = name,
            Resource = resource,
            Action = action,
            GrantedAt = DateTime.UtcNow
        };
    }
}
