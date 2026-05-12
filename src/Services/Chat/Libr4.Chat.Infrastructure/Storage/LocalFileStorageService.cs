using System;
using System.IO;
using System.Threading.Tasks;
using Libr4.Chat.Application.Abstractions;

namespace Libr4.Chat.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService()
    {
        _basePath = Path.Combine(AppContext.BaseDirectory, "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var id = Guid.NewGuid().ToString();
        var ext = Path.GetExtension(fileName);
        var storedFileName = $"{id}{ext}";
        var filePath = Path.Combine(_basePath, storedFileName);

        await using var fs = File.Create(filePath);
        await fileStream.CopyToAsync(fs);

        // Return a relative URL path
        return $"/uploads/{storedFileName}";
    }
}
