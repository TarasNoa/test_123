using Libr4.Chat.Application.Files.Commands;
using Libr4.Shared.Kernel.Results;

namespace Libr4.Chat.Infrastructure.Storage;

public class LocalStorageService : IStorageService
{
    public Task<Result<UploadUrlResponse>> GetPresignedUploadUrlAsync(
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        var fileId = Guid.NewGuid().ToString();
        var uploadUrl = $"/api/chat/files/upload/{fileId}";
        var fileUrl = $"/uploads/{fileId}-{fileName}";

        return Task.FromResult(Result.Success(new UploadUrlResponse(
            UploadUrl: uploadUrl,
            FileUrl: fileUrl,
            FileId: fileId,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15))));
    }
}
