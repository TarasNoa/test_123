using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Libr4.Chat.Application.Files.Commands;
using Libr4.Shared.Kernel.Domain;
using Libr4.Shared.Kernel.Errors;
using Libr4.Shared.Kernel.Results;
using Microsoft.Extensions.Configuration;

namespace Libr4.Chat.Infrastructure.Storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicUrlBase;

    public S3StorageService(IConfiguration configuration)
    {
        var awsKey = configuration["AWS:AccessKey"] ?? throw new InvalidOperationException("AWS:AccessKey not configured");
        var awsSecret = configuration["AWS:SecretKey"] ?? throw new InvalidOperationException("AWS:SecretKey not configured");
        var serviceUrl = configuration["AWS:ServiceURL"]; // For MinIO
        var region = configuration["AWS:Region"] ?? "us-east-1";

        _bucketName = configuration["AWS:BucketName"] ?? "libr4-chat";
        _publicUrlBase = configuration["AWS:PublicUrlBase"] ?? $"{serviceUrl}/{_bucketName}";

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            ForcePathStyle = !string.IsNullOrEmpty(serviceUrl) // MinIO uses path-style
        };

        if (!string.IsNullOrEmpty(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
        }

        _s3Client = new AmazonS3Client(awsKey, awsSecret, config);
    }

    public async Task<Result<UploadUrlResponse>> GetPresignedUploadUrlAsync(
        string fileName,
        string contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileId = Guid.NewGuid().ToString();
            var key = $"uploads/{DateTime.UtcNow:yyyy/MM}/{fileId}-{fileName}";

            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(15),
                ContentType = contentType
            };

            var uploadUrl = _s3Client.GetPreSignedURL(request);
            var fileUrl = $"{_publicUrlBase}/{key}";

            return Result.Success(new UploadUrlResponse(
                UploadUrl: uploadUrl,
                FileUrl: fileUrl,
                FileId: fileId,
                ExpiresAt: request.Expires));
        }
        catch (Exception ex)
        {
            return Result.Failure<UploadUrlResponse>(
                Error.Failure("Storage.Error", $"Failed to generate upload URL: {ex.Message}"));
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _s3Client.ListBucketsAsync(cancellationToken);
            if (response.Buckets.All(b => b.BucketName != _bucketName))
            {
                await _s3Client.PutBucketAsync(_bucketName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Bucket might already exist or we don't have permission to list
            // Continue anyway - the upload will fail if bucket doesn't exist
            // Serilog.Log.Debug(ex, "Failed to list S3 buckets, assuming bucket exists");
        }
    }
}
