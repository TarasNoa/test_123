using System.Net;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Libr4.Shared.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace Libr4.Shared.Web.Results;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess) return Microsoft.AspNetCore.Http.Results.Ok(result.Value);
        return Problem(result.Error);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess) return Microsoft.AspNetCore.Http.Results.NoContent();
        return Problem(result.Error);
    }

    public static IResult Problem(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => HttpStatusCode.BadRequest,
            ErrorType.NotFound => HttpStatusCode.NotFound,
            ErrorType.Conflict => HttpStatusCode.Conflict,
            ErrorType.Unauthorized => HttpStatusCode.Unauthorized,
            ErrorType.Forbidden => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError,
        };

        return Microsoft.AspNetCore.Http.Results.Json(
            new
            {
                type = "about:blank",
                title = error.Code,
                status = (int)status,
                detail = error.Message
            },
            statusCode: (int)status,
            contentType: "application/problem+json");
    }
}
