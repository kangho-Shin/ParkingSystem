using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.SignalR;
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
    private long _registeredKioskDeviceId;

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
            .Build();
        _subscription = _connection.On<KioskExitNotification>("ExitVehicleDetected", DispatchAsync);
        _connection.Closed += error =>
        {
            Log?.Invoke($"EdgeService 연결 종료: {error?.Message}");
            if (!_lifetimeToken.IsCancellationRequested) _ = RestartAsync(_lifetimeToken);
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync(CancellationToken token = default)
    {
        _lifetimeToken = token;
        while (!token.IsCancellationRequested)
        {
            try { await _connection.StartAsync(token); await RegisterWithRetryAsync(token); Log?.Invoke($"EdgeService 연결 완료 (현장 {_sitenum} / 그룹 {_groupnum} / 장비 {_devicenum})"); return; }
            catch (OperationCanceledException ex) when (!token.IsCancellationRequested) { Log?.Invoke($"EdgeService 연결 시간 초과: {ex.Message}. 5초 후 재시도합니다."); await Task.Delay(TimeSpan.FromSeconds(5), token); }
            catch (Exception ex) when (EdgeSignalRRetryPolicy.IsTransient(ex)) { Log?.Invoke($"EdgeService 연결 실패: {ex.Message}. 5초 후 재시도합니다."); await Task.Delay(TimeSpan.FromSeconds(5), token); }
            catch (Exception ex) when (ex is not OperationCanceledException) { Log?.Invoke($"EdgeService 영구 연결 오류: {ex.Message}"); throw; }
        }
    }

    private async Task RestartAsync(CancellationToken token)
    {
        try { await StartAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { Log?.Invoke($"EdgeService 재연결 중단: {ex.Message}"); }
    }

    public Task StopAsync(CancellationToken token = default) => _connection.StopAsync(token);
    private async Task RegisterAsync(CancellationToken token = default)
    {
        long kioskDeviceId = await _connection.InvokeAsync<long>(
            "Register", _sitenum, _groupnum, _devicenum, token);
        if (kioskDeviceId <= 0)
            throw new InvalidDataException("EdgeService가 올바른 내부 단말 ID를 반환하지 않았습니다.");
        Interlocked.Exchange(ref _registeredKioskDeviceId, kioskDeviceId);
        await _connection.InvokeAsync("DeliverPending", token);
    }

    private async Task RegisterWithRetryAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            try { await RegisterAsync(token); return; }
            catch (OperationCanceledException ex) when (!token.IsCancellationRequested) { Log?.Invoke($"장비 등록 시간 초과: {ex.Message}. 5초 후 재시도합니다."); await Task.Delay(TimeSpan.FromSeconds(5), token); }
            catch (Exception ex) when (EdgeSignalRRetryPolicy.IsTransient(ex)) { Log?.Invoke($"장비 등록 실패: {ex.Message}. 5초 후 재시도합니다."); await Task.Delay(TimeSpan.FromSeconds(5), token); }
            catch (Exception ex) when (ex is not OperationCanceledException) { Log?.Invoke($"장비 등록 영구 오류: {ex.Message}"); throw; }
        }
    }

    private async Task DispatchAsync(KioskExitNotification notification)
    {
        if (!MatchesRegistration(
                notification,
                _sitenum,
                _groupnum,
                Interlocked.Read(ref _registeredKioskDeviceId)))
        {
            Log?.Invoke("등록 단말과 일치하지 않는 EdgeService 출차 알림을 무시했습니다.");
            return;
        }
        Func<KioskExitNotification, Task>? handlers = ExitVehicleDetected;
        if (handlers is null) return;
        foreach (Func<KioskExitNotification, Task> handler in handlers.GetInvocationList())
            await handler(notification);
    }

    internal static bool MatchesRegistration(
        KioskExitNotification notification,
        long sitenum,
        int groupnum,
        long kioskDeviceId) =>
        notification.SiteId == sitenum &&
        notification.Groupnum == groupnum &&
        kioskDeviceId > 0 &&
        notification.KioskDeviceId == kioskDeviceId;

    public async ValueTask DisposeAsync()
    {
        _subscription.Dispose();
        await _connection.DisposeAsync();
    }
}

internal static class EdgeSignalRRetryPolicy
{
    public static bool IsTransient(Exception exception) => exception switch
    {
        HubException => false,
        HttpRequestException http => http.StatusCode is null ||
            http.StatusCode == HttpStatusCode.RequestTimeout ||
            http.StatusCode == HttpStatusCode.ServiceUnavailable ||
            http.StatusCode == HttpStatusCode.GatewayTimeout,
        TaskCanceledException => true,
        TimeoutException => true,
        IOException => true,
        WebSocketException => true,
        _ => false
    };
}
