using System.Net;
using System.Text.Json;
using Libr4.Shared.Kernel.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Libr4.Shared.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteProblemAsync(ctx, HttpStatusCode.InternalServerError,
                Error.Failure("internal_error", "An internal error occurred"));
        }
    }

    public static async Task WriteProblemAsync(HttpContext ctx, HttpStatusCode status, Error error)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = "about:blank",
            title = error.Code,
            status = (int)status,
            detail = error.Message,
            traceId = ctx.TraceIdentifier
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
