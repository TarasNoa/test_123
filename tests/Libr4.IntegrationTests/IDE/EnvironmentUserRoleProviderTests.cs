using Libr4.IDE.Application.AutonomousAppGeneration.Tooling.Security;
using Xunit;

namespace Libr4.IntegrationTests.IDE;

public sealed class EnvironmentUserRoleProviderTests
{
    [Fact]
    public void GetCurrentRole_ShouldReturnConfiguredRole_WhenEnvVarIsValid()
    {
        var previous = Environment.GetEnvironmentVariable(EnvironmentUserRoleProvider.EnvVarName);
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentUserRoleProvider.EnvVarName, "InternalDeveloper");
            var provider = new EnvironmentUserRoleProvider(UserRole.ExternalUser);
            Assert.Equal(UserRole.InternalDeveloper, provider.GetCurrentRole());
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentUserRoleProvider.EnvVarName, previous);
        }
    }

    [Fact]
    public void GetCurrentRole_ShouldFallback_WhenEnvVarIsInvalid()
    {
        var previous = Environment.GetEnvironmentVariable(EnvironmentUserRoleProvider.EnvVarName);
        try
        {
            Environment.SetEnvironmentVariable(EnvironmentUserRoleProvider.EnvVarName, "invalid-role");
            var provider = new EnvironmentUserRoleProvider(UserRole.ExternalUser);
            Assert.Equal(UserRole.ExternalUser, provider.GetCurrentRole());
        }
        finally
        {
            Environment.SetEnvironmentVariable(EnvironmentUserRoleProvider.EnvVarName, previous);
        }
    }
}
