using Libr4.Chat.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Libr4.Chat.Infrastructure.Services;

/// <summary>
/// Development stub for WebRTC media service.
/// Replace with real implementation (mediasoup, LiveKit, or Daily.co) in production.
/// </summary>
public sealed class NullMediaService : IMediaService
{
    private readonly ILogger<NullMediaService> _logger;
    public NullMediaService(ILogger<NullMediaService> logger) => _logger = logger;

    public string CreatePeerConnection(string roomId)
    {
        _logger.LogWarning("NullMediaService: CreatePeerConnection called (WebRTC not configured)");
        return $"pc_{roomId}_{Guid.NewGuid():N}";
    }

    public void AddIceCandidate(string roomId, string candidate)
        => _logger.LogDebug("NullMediaService: AddIceCandidate {RoomId}", roomId);

    public string CreateOffer(string roomId)
    {
        _logger.LogWarning("NullMediaService: CreateOffer (WebRTC not configured)");
        return """{"type":"offer","sdp":"v=0\r\no=- 0 0 IN IP4 127.0.0.1\r\n..."}""";
    }

    public void HandleAnswer(string roomId, string answer)
        => _logger.LogDebug("NullMediaService: HandleAnswer {RoomId}", roomId);

    public void CloseConnection(string roomId)
        => _logger.LogDebug("NullMediaService: CloseConnection {RoomId}", roomId);
}
