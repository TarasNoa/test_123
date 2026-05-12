namespace Libr4.Chat.Application.Abstractions;

public interface IMediaService
{
    string CreatePeerConnection(string roomId);
    void AddIceCandidate(string roomId, string candidate);
    string CreateOffer(string roomId);
    void HandleAnswer(string roomId, string answer);
    void CloseConnection(string roomId);
}
