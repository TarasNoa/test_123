using System.Runtime.InteropServices;
using Libr4.Chat.Application.Abstractions;

namespace Libr4.Chat.Application.Services;

public class RustMediaService : IMediaService
{
    [DllImport("media_handler.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr create_peer_connection(IntPtr roomId);

    [DllImport("media_handler.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void add_ice_candidate(IntPtr roomId, IntPtr candidate);

    [DllImport("media_handler.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr create_offer(IntPtr roomId);

    [DllImport("media_handler.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void handle_answer(IntPtr roomId, IntPtr answer);

    [DllImport("media_handler.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void close_connection(IntPtr roomId);

    public string CreatePeerConnection(string roomId)
    {
        var roomIdPtr = Marshal.StringToHGlobalAnsi(roomId);
        var resultPtr = create_peer_connection(roomIdPtr);
        var result = Marshal.PtrToStringAnsi(resultPtr);
        Marshal.FreeHGlobal(roomIdPtr);
        return result!;
    }

    public void AddIceCandidate(string roomId, string candidate)
    {
        var roomIdPtr = Marshal.StringToHGlobalAnsi(roomId);
        var candidatePtr = Marshal.StringToHGlobalAnsi(candidate);
        add_ice_candidate(roomIdPtr, candidatePtr);
        Marshal.FreeHGlobal(roomIdPtr);
        Marshal.FreeHGlobal(candidatePtr);
    }

    public string CreateOffer(string roomId)
    {
        var roomIdPtr = Marshal.StringToHGlobalAnsi(roomId);
        var resultPtr = create_offer(roomIdPtr);
        var result = Marshal.PtrToStringAnsi(resultPtr);
        Marshal.FreeHGlobal(roomIdPtr);
        return result!;
    }

    public void HandleAnswer(string roomId, string answer)
    {
        var roomIdPtr = Marshal.StringToHGlobalAnsi(roomId);
        var answerPtr = Marshal.StringToHGlobalAnsi(answer);
        handle_answer(roomIdPtr, answerPtr);
        Marshal.FreeHGlobal(roomIdPtr);
        Marshal.FreeHGlobal(answerPtr);
    }

    public void CloseConnection(string roomId)
    {
        var roomIdPtr = Marshal.StringToHGlobalAnsi(roomId);
        close_connection(roomIdPtr);
        Marshal.FreeHGlobal(roomIdPtr);
    }
}