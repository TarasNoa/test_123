namespace Libr4.Shared.Contracts.MultiModal;

/// <summary>
/// Multi-modal content type.
/// </summary>
public enum MultiModalContentType
{
    /// <summary>
    /// Text content.
    /// </summary>
    Text,

    /// <summary>
    /// Image content.
    /// </summary>
    Image,

    /// <summary>
    /// Audio content.
    /// </summary>
    Audio,

    /// <summary>
    /// Video content.
    /// </summary>
    Video,

    /// <summary>
    /// Document content (PDF, etc.).
    /// </summary>
    Document
}

/// <summary>
/// Multi-modal content item.
/// </summary>
public record MultiModalContent
{
    /// <summary>
    /// Content type.
    /// </summary>
    public MultiModalContentType Type { get; init; }

    /// <summary>
    /// Text content (if type is Text).
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Image data (base64 encoded).
    /// </summary>
    public string? ImageData { get; init; }

    /// <summary>
    /// Image MIME type.
    /// </summary>
    public string? ImageMimeType { get; init; }

    /// <summary>
    /// Image URL (if hosted).
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// File path (if local).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Content metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Multi-modal message.
/// </summary>
public record MultiModalMessage
{
    /// <summary>
    /// Message role (user, assistant, system).
    /// </summary>
    public string Role { get; init; } = "user";

    /// <summary>
    /// Message content (can be text or multi-modal).
    /// </summary>
    public List<MultiModalContent> Content { get; init; } = new();

    /// <summary>
    /// Message timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Message metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Image analysis result.
/// </summary>
public record ImageAnalysisResult
{
    /// <summary>
    /// Description of the image.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Detected objects.
    /// </summary>
    public List<string> Objects { get; init; } = new();

    /// <summary>
    /// Detected text (OCR).
    /// </summary>
    public string? DetectedText { get; init; }

    /// <summary>
    /// Confidence score.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Additional analysis data.
    /// </summary>
    public Dictionary<string, object> AnalysisData { get; init; } = new();
}

/// <summary>
/// Multi-modal service interface.
/// </summary>
public interface IMultiModalService
{
    /// <summary>
    /// Processes a multi-modal message.
    /// </summary>
    /// <param name="message">Multi-modal message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processed message with text descriptions.</returns>
    Task<MultiModalMessage> ProcessMessageAsync(
        MultiModalMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes an image.
    /// </summary>
    /// <param name="imageData">Base64 encoded image data.</param>
    /// <param name="mimeType">Image MIME type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Image analysis result.</returns>
    Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageData,
        string mimeType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts image to text description.
    /// </summary>
    /// <param name="imageData">Base64 encoded image data.</param>
    /// <param name="mimeType">Image MIME type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Text description of the image.</returns>
    Task<string> ImageToTextAsync(
        string imageData,
        string mimeType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates multi-modal content.
    /// </summary>
    /// <param name="content">Multi-modal content.</param>
    /// <returns>Whether the content is valid.</returns>
    bool ValidateContent(MultiModalContent content);

    /// <summary>
    /// Converts multi-modal message to LLM-compatible format.
    /// </summary>
    /// <param name="message">Multi-modal message.</param>
    /// <returns>LLM-compatible message.</returns>
    object ToLLMFormat(MultiModalMessage message);
}

/// <summary>
/// In-memory multi-modal service for development and testing.
/// </summary>
public class InMemoryMultiModalService : IMultiModalService
{
    public async Task<MultiModalMessage> ProcessMessageAsync(
        MultiModalMessage message,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var processedContent = new List<MultiModalContent>();

        foreach (var content in message.Content)
        {
            if (!ValidateContent(content))
            {
                continue;
            }

            if (content.Type == MultiModalContentType.Image)
            {
                // Convert image to text description
                var description = await ImageToTextAsync(
                    content.ImageData ?? "",
                    content.ImageMimeType ?? "image/png",
                    cancellationToken);

                processedContent.Add(new MultiModalContent
                {
                    Type = MultiModalContentType.Text,
                    Text = $"[Image: {description}]"
                });
            }
            else
            {
                processedContent.Add(content);
            }
        }

        return message with { Content = processedContent };
    }

    public async Task<ImageAnalysisResult> AnalyzeImageAsync(
        string imageData,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        // Mock implementation - in production, this would call a vision model
        return new ImageAnalysisResult
        {
            Description = "An image showing a user interface or code snippet",
            Objects = new List<string> { "interface", "text", "code" },
            DetectedText = null,
            Confidence = 0.8f,
            AnalysisData = new Dictionary<string, object>
            {
                ["width"] = 1024,
                ["height"] = 768,
                ["format"] = mimeType
            }
        };
    }

    public async Task<string> ImageToTextAsync(
        string imageData,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        // Mock implementation - in production, this would call a vision model
        return "A screenshot showing a code editor with syntax highlighting";
    }

    public bool ValidateContent(MultiModalContent content)
    {
        return content.Type switch
        {
            MultiModalContentType.Text => !string.IsNullOrWhiteSpace(content.Text),
            MultiModalContentType.Image => !string.IsNullOrWhiteSpace(content.ImageData) || 
                                            !string.IsNullOrWhiteSpace(content.ImageUrl),
            MultiModalContentType.Audio => !string.IsNullOrWhiteSpace(content.FilePath),
            MultiModalContentType.Video => !string.IsNullOrWhiteSpace(content.FilePath),
            MultiModalContentType.Document => !string.IsNullOrWhiteSpace(content.FilePath),
            _ => false
        };
    }

    public object ToLLMFormat(MultiModalMessage message)
    {
        // Convert to format compatible with multi-modal LLMs
        var content = new List<object>();

        foreach (var item in message.Content)
        {
            if (item.Type == MultiModalContentType.Text)
            {
                content.Add(new { type = "text", text = item.Text });
            }
            else if (item.Type == MultiModalContentType.Image)
            {
                if (!string.IsNullOrEmpty(item.ImageUrl))
                {
                    content.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = item.ImageUrl }
                    });
                }
                else if (!string.IsNullOrEmpty(item.ImageData))
                {
                    content.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:{item.ImageMimeType};base64,{item.ImageData}" }
                    });
                }
            }
        }

        return new
        {
            role = message.Role,
            content
        };
    }
}

/// <summary>
/// Multi-modal message builder.
/// </summary>
public class MultiModalMessageBuilder
{
    private readonly List<MultiModalContent> _content = new();
    private string _role = "user";
    private readonly Dictionary<string, string> _metadata = new();

    public MultiModalMessageBuilder WithRole(string role)
    {
        _role = role;
        return this;
    }

    public MultiModalMessageBuilder WithText(string text)
    {
        _content.Add(new MultiModalContent
        {
            Type = MultiModalContentType.Text,
            Text = text
        });
        return this;
    }

    public MultiModalMessageBuilder WithImage(string imageData, string mimeType = "image/png")
    {
        _content.Add(new MultiModalContent
        {
            Type = MultiModalContentType.Image,
            ImageData = imageData,
            ImageMimeType = mimeType
        });
        return this;
    }

    public MultiModalMessageBuilder WithImageUrl(string imageUrl)
    {
        _content.Add(new MultiModalContent
        {
            Type = MultiModalContentType.Image,
            ImageUrl = imageUrl
        });
        return this;
    }

    public MultiModalMessageBuilder WithImageFile(string filePath)
    {
        _content.Add(new MultiModalContent
        {
            Type = MultiModalContentType.Image,
            FilePath = filePath
        });
        return this;
    }

    public MultiModalMessageBuilder WithMetadata(string key, string value)
    {
        _metadata[key] = value;
        return this;
    }

    public MultiModalMessage Build()
    {
        return new MultiModalMessage
        {
            Role = _role,
            Content = _content,
            Metadata = _metadata
        };
    }
}

/// <summary>
/// Image processing utilities.
/// </summary>
public static class ImageProcessingUtils
{
    /// <summary>
    /// Converts an image file to base64.
    /// </summary>
    public static async Task<string> ImageToBase64Async(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Image file not found: {filePath}");
        }

        var bytes = await File.ReadAllBytesAsync(filePath);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Gets MIME type from file extension.
    /// </summary>
    public static string GetMimeTypeFromExtension(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            _ => "image/png"
        };
    }

    /// <summary>
    /// Validates image data.
    /// </summary>
    public static bool ValidateImageData(string imageData, string mimeType)
    {
        if (string.IsNullOrWhiteSpace(imageData))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(imageData);
            return bytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resizes image (placeholder implementation).
    /// </summary>
    public static async Task<string> ResizeImageAsync(
        string imageData,
        int maxWidth,
        int maxHeight,
        string mimeType = "image/png")
    {
        // Placeholder - in production, this would use an image processing library
        await Task.CompletedTask;
        return imageData;
    }
}
