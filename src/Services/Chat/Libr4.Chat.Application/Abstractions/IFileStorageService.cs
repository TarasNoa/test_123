using System.IO;
using System.Threading.Tasks;

namespace Libr4.Chat.Application.Abstractions;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
}
