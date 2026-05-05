namespace Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;

public enum UserRole
{
    ExternalUser,
    InternalDeveloper,
    Operator
}

public interface IUserRoleProvider
{
    UserRole GetCurrentRole();
}

public sealed class StaticUserRoleProvider : IUserRoleProvider
{
    private readonly UserRole _role;

    public StaticUserRoleProvider(UserRole role = UserRole.ExternalUser)
    {
        _role = role;
    }

    public UserRole GetCurrentRole() => _role;
}

public sealed class EnvironmentUserRoleProvider : IUserRoleProvider
{
    public const string EnvVarName = "AUTONOMOUS_USER_ROLE";
    private readonly UserRole _fallbackRole;

    public EnvironmentUserRoleProvider(UserRole fallbackRole = UserRole.ExternalUser)
    {
        _fallbackRole = fallbackRole;
    }

    public UserRole GetCurrentRole()
    {
        var raw = Environment.GetEnvironmentVariable(EnvVarName);
        if (string.IsNullOrWhiteSpace(raw))
            return _fallbackRole;

        return Enum.TryParse<UserRole>(raw.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : _fallbackRole;
    }
}
