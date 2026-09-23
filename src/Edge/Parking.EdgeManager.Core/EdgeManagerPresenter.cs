using Parking.Contracts;

namespace Parking.EdgeManager.Core;

public sealed class EdgeManagerPresenter : IDisposable
{
    private readonly IEdgeManagementClient _client;
    private readonly IEdgeManagerView _view;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _latestEventKey;
    private DateTimeOffset? _configurationSyncedAt;
    private bool _configurationLoaded;
    private IReadOnlyList<EdgeEntryItem> _entries = Array.Empty<EdgeEntryItem>();
    private IReadOnlyList<EdgeActivityItem> _activities = Array.Empty<EdgeActivityItem>();
    private CancellationTokenSource? _imageCancellation;

    public EdgeManagerPresenter(IEdgeManagementClient client, IEdgeManagerView view)
    {
        _client = client;
        _view = view;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (!await _refreshLock.WaitAsync(0, cancellationToken))
            return;

        try
        {
            EdgeServiceStatus status = await _client.GetStatusAsync(cancellationToken);
            IReadOnlyList<EdgeEntryItem> entries =
                await _client.GetEntriesAsync(cancellationToken);
            IReadOnlyList<EdgeActivityItem> activities =
                await _client.GetActivitiesAsync(cancellationToken);

            _entries = entries;
            _activities = activities;
            _view.ShowStatus(status);
            _view.ShowEntries(entries);
            _view.ShowActivities(activities);

            if (!_configurationLoaded || _configurationSyncedAt != status.ConfigurationSyncedAt)
            {
                _view.ShowConfiguration(
                    await _client.GetConfigurationAsync(cancellationToken));
                _configurationSyncedAt = status.ConfigurationSyncedAt;
                _configurationLoaded = true;
            }

            EdgeEntryItem? entry = entries.OrderByDescending(x => x.InDateTime).FirstOrDefault();
            EdgeActivityItem? activity = activities.OrderByDescending(x => x.OccurredAt).FirstOrDefault();
            bool activityIsLatest = activity is not null &&
                (entry is null || activity.OccurredAt >= entry.InDateTime);
            string? eventKey = activityIsLatest
                ? $"A:{activity!.ActivityType}:{activity.ActivityId:D}:{activity.OccurredAt:O}"
                : entry is null
                    ? null
                    : $"E:{entry.EventId:D}:{entry.InDateTime:O}";

            if (eventKey is not null && eventKey != _latestEventKey)
            {
                if (activityIsLatest)
                    await ShowActivityAsync(activity!, cancellationToken);
                else
                    await ShowEntryAsync(entry!, cancellationToken);
                _latestEventKey = eventKey;
            }
        }
        catch (Exception exception) when (
            exception is HttpRequestException ||
            exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _view.ShowDisconnected();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public Task SelectEntryAsync(Guid eventId, CancellationToken cancellationToken)
    {
        EdgeEntryItem? entry = _entries.FirstOrDefault(x => x.EventId == eventId);
        return entry is null ? Task.CompletedTask : ShowEntryAsync(entry, cancellationToken);
    }

    public Task SelectActivityAsync(
        Guid activityId,
        EdgeActivityType activityType,
        CancellationToken cancellationToken)
    {
        EdgeActivityItem? activity = _activities.FirstOrDefault(
            x => x.ActivityId == activityId && x.ActivityType == activityType);
        return activity is null
            ? Task.CompletedTask
            : ShowActivityAsync(activity, cancellationToken);
    }

    private async Task ShowEntryAsync(
        EdgeEntryItem entry,
        CancellationToken cancellationToken)
    {
        CancellationToken token = BeginImageRequest(cancellationToken);
        try
        {
            byte[]? inImage = await _client.GetImageAsync(entry.InImage, token);
            if (!token.IsCancellationRequested) _view.ShowImages(inImage, null);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (TaskCanceledException) { }
        catch (HttpRequestException) { }
    }

    private async Task ShowActivityAsync(
        EdgeActivityItem activity,
        CancellationToken cancellationToken)
    {
        CancellationToken token = BeginImageRequest(cancellationToken);
        try
        {
            Task<byte[]?> inTask = _client.GetImageAsync(activity.InImage, token);
            Task<byte[]?> outTask = _client.GetImageAsync(activity.OutImage, token);
            await Task.WhenAll(inTask, outTask);
            if (!token.IsCancellationRequested)
                _view.ShowImages(await inTask, await outTask);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (TaskCanceledException) { }
        catch (HttpRequestException) { }
    }

    private CancellationToken BeginImageRequest(CancellationToken cancellationToken)
    {
        _imageCancellation?.Cancel();
        _imageCancellation?.Dispose();
        _imageCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return _imageCancellation.Token;
    }

    public void Dispose()
    {
        _imageCancellation?.Cancel();
        _imageCancellation?.Dispose();
        _refreshLock.Dispose();
    }
}
