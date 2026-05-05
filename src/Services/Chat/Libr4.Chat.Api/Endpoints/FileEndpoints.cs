using Libr4.Chat.Application.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Chat.Api.Endpoints;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/files")
            .WithTags("Files")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/upload-url", async (
            [FromBody] GetUploadUrlRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetUploadUrlCommand(request.FileName, request.ContentType, request.FileSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        return app;
    }
}

public record GetUploadUrlRequest(string FileName, string ContentType, long FileSize);
