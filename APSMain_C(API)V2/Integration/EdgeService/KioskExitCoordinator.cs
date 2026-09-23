namespace APSMain.Integration.EdgeService;

public sealed class KioskExitCoordinator
{
    private readonly EdgeServiceClient _client;
    private readonly KioskEventTracker _tracker;
    private readonly EdgeServiceOptions _options;
    private readonly TimeSpan _retryDelay;
    public event Func<KioskExitContext, Task>? SearchResolved;
    public event Action<string>? Error;

    public KioskExitCoordinator(
        EdgeServiceClient client,
        KioskEventTracker tracker,
        EdgeServiceOptions options,
        TimeSpan? retryDelay = null)
    {
        _client = client;
        _tracker = tracker;
        _options = options;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(5);
    }

    public async Task HandleAsync(KioskExitNotification notification, CancellationToken token = default)
    {
        Validate(notification);
        if (!_tracker.TryBegin(notification.EventId)) return;
        try
        {
            EdgeCallResult<ParkingSearchResult> result;
            while (true)
            {
                result = await _client.SearchParkingAsync(notification.CarNumber, notification.OutDateTime, token);
                if (result.IsSuccess && result.Value is not null) break;
                if (result.Status != EdgeCallStatus.TransientFailure)
                    throw new InvalidOperationException(result.Error ?? "차량검색 요청이 거부되었습니다.");
                Error?.Invoke(result.Error ?? "차량검색에 실패했습니다. 5초 후 다시 시도합니다.");
                await Task.Delay(_retryDelay, token);
            }

            KioskExitContext context = new()
            {
                Notification = notification,
                Quote = result.Value.Quote,
                Candidates = result.Value.Candidates,
                ParkingSessionId = result.Value.Quote?.ParkingSessionId,
                IsEventDriven = true
            };
            Func<KioskExitContext, Task>? handlers = SearchResolved;
            if (handlers is not null)
                foreach (Func<KioskExitContext, Task> handler in handlers.GetInvocationList()) await handler(context);
            _tracker.MarkCompleted(notification.EventId);
        }
        catch
        {
            _tracker.MarkFailed(notification.EventId);
            throw;
        }
    }

    private void Validate(KioskExitNotification notification)
    {
        if (notification.EventId == Guid.Empty ||
            notification.SiteId != _options.Sitenum ||
            notification.Groupnum != _options.Groupnum ||
            notification.LprDeviceId <= 0 ||
            notification.KioskDeviceId <= 0 ||
            string.IsNullOrWhiteSpace(notification.CarNumber) ||
            notification.OutDateTime == default)
            throw new InvalidOperationException("EdgeService 출차 알림 식별값이 올바르지 않습니다.");
    }
}
