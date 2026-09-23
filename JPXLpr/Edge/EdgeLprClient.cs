using System.Net.Sockets;

namespace JPXLpr.Edge;

public sealed record EdgeLprSendResult(
    bool Accepted,
    string Code,
    string Message,
    string? ResultCode = null);

public interface IEdgeLprSender
{
    Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token);
}

public sealed class EdgeLprClient : IEdgeLprSender
{
    private readonly EdgeLprOptions _options;

    public EdgeLprClient(EdgeLprOptions options)
    {
        options.Validate();
        _options = options;
    }

    public async Task<EdgeLprSendResult> SendAsync(EdgeLprEvent value, CancellationToken token)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            using TcpClient tcp = new();
            await tcp.ConnectAsync(_options.Host, _options.Port, timeout.Token);
            using NetworkStream stream = tcp.GetStream();
            await stream.WriteAsync(EdgeLprFrame.Encode(value.FileName), timeout.Token);
            await stream.FlushAsync(timeout.Token);
            string response = await EdgeLprResponseParser.ReadAsync(stream, timeout.Token);
            return EdgeLprResponseParser.Match(response, value.EventId);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(false, "TIMEOUT", "EdgeService 응답 시간 초과");
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            return new(false, "CONNECTION_ERROR", ex.Message);
        }
    }
}
