using System;

namespace Libr4.Auth.Domain.Users;

public class Role
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public List<string> Permissions { get; private set; } = new();

    private Role() { }

    public static Role Create(string name, string description, List<string> permissions)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Permissions = permissions ?? new List<string>()
        };
    }
}

public enum RoleType
{
    User = 0,
    Admin = 1,
    Support = 2,
    Trader = 3,
    Freelancer = 4,
    Client = 5,
}

public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Role Role { get; private set; }

    private UserRole() { }
    public UserRole(Guid userId, Role role)
    {
        UserId = userId;
        Role = role;
    }
}
