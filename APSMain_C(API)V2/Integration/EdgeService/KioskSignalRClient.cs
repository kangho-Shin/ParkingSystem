using Microsoft.AspNetCore.SignalR.Client;

namespace APSMain.Integration.EdgeService;

public sealed class KioskSignalRClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly long _sitenum;
    private readonly int _groupnum;
    private readonly int _devicenum;
    private readonly IDisposable _subscription;
    private CancellationToken _lifetimeToken;

    public event Func<KioskExitNotification, Task>? ExitVehicleDetected;
    public event Action<string>? Log;
    internal (long Sitenum, int Groupnum, int Devicenum) RegistrationIdentity =>
        (_sitenum, _groupnum, _devicenum);

    public KioskSignalRClient(EdgeServiceOptions options)
    {
        _sitenum = options.Sitenum;
        _groupnum = options.Groupnum;
        _devicenum = options.Devicenum;
        _connection = new HubConnectionBuilder()
            .WithUrl(new Uri(options.BaseAddress, "hubs/kiosk"))
            .WithAutomaticReconnect()
            .Build();
        _subscription = _connection.On<KioskExitNotification>("ExitVehicleDetected", DispatchAsync);
        _connection.Reconnecting += error => { Log?.Invoke($"EdgeService 재연결 중: {error?.Message}"); return Task.CompletedTask; };
        _connection.Reconnected += async _ => { await RegisterWithRetryAsync(_lifetimeToken); Log?.Invoke("EdgeService 재연결 및 장비 재등록 완료"); };
        _connection.Closed += error => { Log?.Invoke($"EdgeService 연결 종료: {error?.Message}"); if (!_lifetimeToken.IsCancellationRequested) _ = StartAsync(_lifetimeToken); return Task.CompletedTask; };
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        _lifetimeToken = token;
        while (!token.IsCancellationRequested)
        {
            try { await _connection.StartAsync(token); await RegisterWithRetryAsync(token); Log?.Invoke($"EdgeService 연결 완료 (현장 {_sitenum} / 그룹 {_groupnum} / 장비 {_devicenum})"); return; }
            catch (Exception ex) when (ex is not OperationCanceledException) { Log?.Invoke($"EdgeService 연결 실패: {ex.Message}. 5초 후 재시도합니다."); await Task.Delay(TimeSpan.FromSeconds(5), token); }
        }
    }

    public Task StopAsync(CancellationToken token = default) => _connection.StopAsync(token);
    private Task RegisterAsync(CancellationToken token = default) =>
        _connection.InvokeAsync("Register", _sitenum, _groupnum, _devicenum, token);
    private async Task RegisterWithRetryAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            try { await RegisterAsync(token); return; }
            catch (Exception ex) when (ex is not OperationCanceledException) { Log?.Invoke($"장비 등록 실패: {ex.Message}. 5초 후 재시도합니다."); await Task.Delay(TimeSpan.FromSeconds(5), token); }
        }
    }

    private async Task DispatchAsync(KioskExitNotification notification)
    {
        Func<KioskExitNotification, Task>? handlers = ExitVehicleDetected;
        if (handlers is null) return;
        foreach (Func<KioskExitNotification, Task> handler in handlers.GetInvocationList())
            await handler(notification);
    }

    public async ValueTask DisposeAsync()
    {
        _subscription.Dispose();
        await _connection.DisposeAsync();
    }
}
