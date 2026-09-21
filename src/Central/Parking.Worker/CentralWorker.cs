using Microsoft.Extensions.Options;

namespace Parking.Worker;

public sealed class CentralWorker : BackgroundService
{
    private readonly ParkingApiHealthClient _client;
    private readonly IOptionsMonitor<ParkingApiOptions> _options;
    private readonly ILogger<CentralWorker> _logger;

    public CentralWorker(
        ParkingApiHealthClient client,
        IOptionsMonitor<ParkingApiOptions> options,
        ILogger<CentralWorker> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parking.Worker가 시작되었습니다.");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parking.Worker가 종료됩니다.");
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool connected = await _client.CheckAsync(stoppingToken);
                _logger.LogInformation("Parking.Api 상태: {Status}", connected ? "연결 정상" : "응답 오류");
            }
            catch (Exception exception) when (
                exception is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(exception, "Parking.Api 상태 확인에 실패했습니다.");
            }

            int seconds = Math.Max(_options.CurrentValue.CheckIntervalSeconds, 1);
            await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
        }
    }
}
