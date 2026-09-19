using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Parking.EdgeService;

public sealed class LprTcpWorker : BackgroundService
{
    private readonly int _port;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LprTcpWorker> _logger;
    private readonly ConcurrentDictionary<int, Task> _clients = new();
    private int _clientId;

    public LprTcpWorker(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<LprTcpWorker> logger)
    {
        _port = configuration.GetValue("Edge:LprListenPort", 29200);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_port == 0)
            return;

        TcpListener listener = new(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("LPR TCP 수신 시작: Port={Port}", _port);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                int clientId = Interlocked.Increment(ref _clientId);
                Task task = HandleClientAsync(client, stoppingToken);
                _clients[clientId] = task;
                _ = task.ContinueWith(
                    completedTask =>
                    {
                        _clients.TryRemove(clientId, out _);
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
            await Task.WhenAll(_clients.Values.ToArray());
            _logger.LogInformation("LPR TCP 수신 종료: Port={Port}", _port);
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        using (client)
        using (IServiceScope scope = _scopeFactory.CreateScope())
        {
            LprLaneProcessor processor =
                scope.ServiceProvider.GetRequiredService<LprLaneProcessor>();
            LprFrameParser parser = new();
            byte[] buffer = new byte[4096];
            try
            {
                NetworkStream stream = client.GetStream();
                while (!cancellationToken.IsCancellationRequested)
                {
                    int count = await stream.ReadAsync(buffer, cancellationToken);
                    if (count == 0)
                        return;

                    IReadOnlyList<LprFrameResult> frames =
                        parser.Append(buffer.AsSpan(0, count));
                    foreach (LprFrameResult frame in frames)
                    {
                        byte[] reply;
                        if (frame.ErrorCode is not null)
                        {
                            reply = LprProtocol.CreateReply(false, null, frame.ErrorCode);
                        }
                        else
                        {
                            LprDecodeResult decoded = LprProtocol.Decode(frame.Data!);
                            if (!decoded.Success)
                            {
                                reply = LprProtocol.CreateReply(
                                    false, null, decoded.ErrorCode);
                            }
                            else
                            {
                                LprProcessResult result = await processor.ProcessAsync(
                                    decoded.Data!, cancellationToken);
                                reply = LprProtocol.CreateReply(
                                    result.Acknowledged,
                                    result.EventId,
                                    result.ErrorCode);
                            }
                        }
                        await stream.WriteAsync(reply, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception) when (
                exception is IOException || exception is SocketException)
            {
                _logger.LogInformation("LPR TCP 연결 종료: {Message}", exception.Message);
            }
        }
    }
}
