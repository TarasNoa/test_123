using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Libr4.Shared.Web;

public static class CurrentUserHttpContextExtensions
{
    public static Guid GetUserId(this HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? httpContext.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    public static Guid GetUserIdOrThrow(this HttpContext httpContext)
    {
        var id = GetUserId(httpContext);
        if (id == Guid.Empty) throw new UnauthorizedAccessException("User not authenticated");
        return id;
    }
}
