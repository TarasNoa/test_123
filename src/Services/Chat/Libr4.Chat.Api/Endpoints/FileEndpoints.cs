using Libr4.Chat.Application.Files.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Libr4.Chat.Api.Endpoints;

public static class FileEndpoints
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat/files")
            .WithTags("Files")
            .RequireAuthorization();

        group.MapPost("/upload", async (
            IFormFile file,
            IFileStorageService storageService) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "File is required" });
            }

            try
            {
                var fileUrl = await storageService.UploadFileAsync(file);
                return Results.Ok(new { url = fileUrl, fileName = file.FileName, size = file.Length, contentType = file.ContentType });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: $"Failed to upload file: {ex.Message}",
                    statusCode: 500,
                    title: "File Upload Error");
            }
        })
        .WithName("UploadFile")
        .WithSummary("Upload a file for chat messages")
        .DisableRateLimiting(); // Allow larger uploads
    }
}
