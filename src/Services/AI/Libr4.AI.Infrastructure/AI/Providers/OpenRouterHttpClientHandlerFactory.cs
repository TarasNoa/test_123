using System.Net;
using System.Net.Sockets;

namespace Libr4.AI.Infrastructure.AI.Providers;

/// <summary>
/// OpenRouter sits behind Cloudflare; on networks with broken IPv6, .NET Happy Eyeballs
/// can hang or fail with "response ended prematurely" while IPv4 works fine.
/// </summary>
public static class OpenRouterHttpClientHandlerFactory
{
    public static SocketsHttpHandler Create() => new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        EnableMultipleHttp2Connections = false,
        ConnectCallback = ConnectIpv4Async
    };

    private static async ValueTask<Stream> ConnectIpv4Async(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var port = context.DnsEndPoint.Port;

        var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
        var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
            ?? throw new HttpRequestException(
                $"No IPv4 address for {host}. OpenRouter requires working IPv4 connectivity.");

        var socket = new Socket(ipv4.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(ipv4, port), cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
