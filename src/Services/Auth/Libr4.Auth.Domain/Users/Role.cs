namespace Libr4.Auth.Domain.Users;

public enum Role
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
