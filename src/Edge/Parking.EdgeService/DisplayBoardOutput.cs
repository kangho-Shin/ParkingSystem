using System.Collections.Concurrent;
using System.Net.Sockets;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class DisplayBoardOutput : IDisplayBoardOutput, IAsyncDisposable
{
    private readonly LocalConfigurationStore _configurationStore;
    private readonly ILogger<DisplayBoardOutput> _logger;
    private readonly ConcurrentDictionary<long, DisplayBoardConnection> _connections = new();
    private readonly DisplayBoardClockState _clockState = new();

    public DisplayBoardOutput(
        LocalConfigurationStore configurationStore,
        ILogger<DisplayBoardOutput> logger)
    {
        _configurationStore = configurationStore;
        _logger = logger;
    }

    public async Task SendClockAsync(
        long siteId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await _configurationStore.GetAsync(
            siteId, cancellationToken);
        if (configuration is null) return;
        foreach (ParkingDevice display in configuration.Devices.Where(x =>
            x.Enabled && string.Equals(x.DeviceType, "LDM", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(x.IpAddress) && x.Port is not null))
        {
            if (!_clockState.ShouldSend(display.DeviceId, now)) continue;
            DisplayBoardConnection connection = GetConnection(display);
            try
            {
                await connection.SendAsync(
                    DisplayBoardProtocol.CreateClockLine(now.LocalDateTime), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                connection.Close();
                _clockState.Reset(display.DeviceId);
                _logger.LogWarning(exception,
                    "전광판 시계 전송 실패: DeviceId={DeviceId}", display.DeviceId);
            }
        }
    }

    public async Task SendAsync(
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken)
        => await SendFromDeviceAsync(
            recognition.DeviceId, recognition, response, cancellationToken);

    public async Task SendFromDeviceAsync(
        long sourceDeviceId,
        LprRecognition recognition,
        FieldEventResponse response,
        CancellationToken cancellationToken)
    {
        SiteConfiguration? configuration = await _configurationStore.GetAsync(
            recognition.SiteId, cancellationToken);
        ParkingDevice? display = configuration is null
            ? null
            : DisplayBoardSelector.Find(configuration, sourceDeviceId);
        if (display is null || string.IsNullOrWhiteSpace(display.IpAddress) || display.Port is null)
        {
            _logger.LogWarning(
                "LPR 카메라 연결 전광판 설정 없음: DeviceId={DeviceId}",
                sourceDeviceId);
            return;
        }

        DisplayBoardConnection connection = GetConnection(display);
        try
        {
            await connection.SendAsync(
                DisplayBoardProtocol.CreateTwoLine(recognition.CarNumber, response.DisplayMessage),
                cancellationToken);
            if (response.OpenBarrier)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                await connection.SendAsync(DisplayBoardProtocol.GateOpen.ToArray(), cancellationToken);
            }
            _clockState.Suppress(display.DeviceId, DateTimeOffset.Now, TimeSpan.FromSeconds(11));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            connection.Close();
            _logger.LogWarning(exception,
                "전광판 전송 실패: DeviceId={DeviceId}, LaneId={LaneId}",
                display.DeviceId, recognition.LaneId);
        }
    }

    private DisplayBoardConnection GetConnection(ParkingDevice display) =>
        _connections.AddOrUpdate(
            display.DeviceId,
            _ => new DisplayBoardConnection(display.IpAddress!, display.Port!.Value),
            (_, current) => ReplaceIfChanged(current, display.IpAddress!, display.Port!.Value));

    private static DisplayBoardConnection ReplaceIfChanged(
        DisplayBoardConnection current, string host, int port)
    {
        if (current.Matches(host, port)) return current;
        current.Close();
        return new DisplayBoardConnection(host, port);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (DisplayBoardConnection connection in _connections.Values)
            await connection.DisposeAsync();
    }

    private sealed class DisplayBoardConnection : IAsyncDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private TcpClient? _client;

        public DisplayBoardConnection(string host, int port) { _host = host; _port = port; }
        public bool Matches(string host, int port) => _host == host && _port == port;

        public async Task SendAsync(byte[] packet, CancellationToken cancellationToken)
        {
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                if (_client is null || !_client.Connected)
                {
                    Close();
                    _client = new TcpClient { NoDelay = true };
                    await _client.ConnectAsync(_host, _port, cancellationToken);
                }
                await _client.GetStream().WriteAsync(packet, cancellationToken);
            }
            finally { _sendLock.Release(); }
        }

        public void Close()
        {
            _client?.Dispose();
            _client = null;
        }

        public ValueTask DisposeAsync()
        {
            Close();
            _sendLock.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
