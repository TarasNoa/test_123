using FluentValidation;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using MediatR;

namespace Libr4.Chat.Application.Files.Commands;

public record GetUploadUrlCommand(
    string FileName,
    string ContentType,
    long FileSize) : IRequest<Result<UploadUrlResponse>>;

public record UploadUrlResponse(
    string UploadUrl,
    string FileUrl,
    string FileId,
    DateTime ExpiresAt);

public class GetUploadUrlValidator : AbstractValidator<GetUploadUrlCommand>
{
    private readonly HashSet<string> _allowedTypes = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf", "text/plain", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public GetUploadUrlValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(type => _allowedTypes.Contains(type.ToLower()))
            .WithMessage("File type not allowed");

        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(50 * 1024 * 1024) // 50MB
            .WithMessage("File size must not exceed 50MB");
    }
}

public class GetUploadUrlHandler : IRequestHandler<GetUploadUrlCommand, Result<UploadUrlResponse>>
{
    private readonly IStorageService _storage;

    public GetUploadUrlHandler(IStorageService storage)
    {
        _storage = storage;
    }

    public async Task<Result<UploadUrlResponse>> Handle(GetUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var result = await _storage.GetPresignedUploadUrlAsync(
            request.FileName,
            request.ContentType,
            request.FileSize,
            cancellationToken);

        return result.IsSuccess 
            ? Result.Success(result.Value) 
            : Result.Failure<UploadUrlResponse>(result.Error);
    }
}

public interface IStorageService
{
    Task<Result<UploadUrlResponse>> GetPresignedUploadUrlAsync(
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default);
}
