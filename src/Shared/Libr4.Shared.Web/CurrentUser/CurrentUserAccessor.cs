using System.Security.Claims;
using Libr4.Shared.Kernel.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Libr4.Shared.Web.CurrentUser;

public sealed class CurrentUserAccessor : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserAccessor(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue("email");

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role)
            .Concat(Principal.FindAll("role"))
            .Select(c => c.Value)
            .Distinct()
            .ToList()
        ?? (IReadOnlyCollection<string>)Array.Empty<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}

public static class CurrentUserExtensions
{
    public static IServiceCollection AddLibr4CurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        return services;
    }
}
