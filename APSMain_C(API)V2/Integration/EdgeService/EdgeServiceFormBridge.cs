using System.Configuration;

namespace APSMain.Integration.EdgeService;

public sealed class EdgeServiceFormBridge : IAsyncDisposable
{
    private readonly KioskSignalRClient _signalR;
    private readonly KioskExitCoordinator _coordinator;
    private readonly HttpClient _http;
    private readonly CancellationTokenSource _shutdown = new();

    public event Func<KioskExitContext, Task>? SearchResolved;
    public event Action<string>? Log;

    public EdgeServiceFormBridge()
    {
        EdgeServiceOptions options = EdgeServiceOptions.Load(ConfigurationManager.AppSettings);
        _http = new HttpClient { BaseAddress = options.BaseAddress };
        Client = new EdgeServiceClient(_http, options);
        _coordinator = new KioskExitCoordinator(
            Client, new KioskEventTracker(), options);
        _signalR = new KioskSignalRClient(options);
        _signalR.ExitVehicleDetected += OnExitVehicleDetectedAsync;
        _signalR.Log += message => Log?.Invoke(message);
        _coordinator.Error += message => Log?.Invoke(message);
        _coordinator.SearchResolved += context => SearchResolved?.Invoke(context) ?? Task.CompletedTask;
    }

    public EdgeServiceClient Client { get; }

    public Task StartAsync() => _signalR.StartAsync(_shutdown.Token);
    private Task OnExitVehicleDetectedAsync(KioskExitNotification notification) => _coordinator.HandleAsync(notification, _shutdown.Token);

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        try { await _signalR.StopAsync(); } catch { }
        await _signalR.DisposeAsync();
        _http.Dispose();
        _shutdown.Dispose();
    }
}
