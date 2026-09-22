namespace Parking.EdgeService;

public sealed class DisplayBoardClockWorker : BackgroundService
{
    private readonly LocalBootstrapStore _bootstrapStore;
    private readonly IDisplayBoardOutput _display;
    private readonly ILogger<DisplayBoardClockWorker> _logger;

    public DisplayBoardClockWorker(
        LocalBootstrapStore bootstrapStore,
        IDisplayBoardOutput display,
        ILogger<DisplayBoardClockWorker> logger)
    {
        _bootstrapStore = bootstrapStore;
        _display = display;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Parking.Contracts.EdgeBootstrapSettings? settings =
                    await _bootstrapStore.GetAsync(stoppingToken);
                if (settings is not null)
                    await _display.SendClockAsync(
                        settings.SiteId, DateTimeOffset.Now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "전광판 시계 전송 실패");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
