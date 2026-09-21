namespace APSMain.Integration.EdgeService;

public sealed class KioskExitCoordinator
{
    private readonly EdgeServiceClient _client;
    private readonly KioskEventTracker _tracker;
    public event Func<KioskExitContext, Task>? SearchResolved;
    public event Action<string>? Error;

    public KioskExitCoordinator(EdgeServiceClient client, KioskEventTracker tracker)
    {
        _client = client;
        _tracker = tracker;
    }

    public async Task HandleAsync(KioskExitNotification notification, CancellationToken token = default)
    {
        if (!_tracker.TryBegin(notification.EventId)) return;
        try
        {
            EdgeCallResult<ParkingSearchResult> result;
            while (true)
            {
                result = await _client.SearchParkingAsync(notification.CarNumber, notification.OutDateTime, token);
                if (result.IsSuccess && result.Value is not null) break;
                Error?.Invoke(result.Error ?? "차량검색에 실패했습니다. 5초 후 다시 시도합니다.");
                await Task.Delay(TimeSpan.FromSeconds(5), token);
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
}
