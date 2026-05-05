using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;

namespace Libr4.Shared.Web.Auth;

/// <summary>
/// Lightweight injectable user identity derived from the JWT claims.
/// Bind via minimal API parameter: <c>CurrentUser user</c>.
/// </summary>
public sealed class CurrentUser
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();

    public static ValueTask<CurrentUser?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var principal = context.User;
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        Guid.TryParse(sub, out var id);

        var user = new CurrentUser
        {
            Id = id,
            Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email"),
            Roles = principal.FindAll(ClaimTypes.Role)
                .Concat(principal.FindAll("role"))
                .Select(c => c.Value)
                .Distinct()
                .ToArray()
        };
        return ValueTask.FromResult<CurrentUser?>(user);
    }
}
