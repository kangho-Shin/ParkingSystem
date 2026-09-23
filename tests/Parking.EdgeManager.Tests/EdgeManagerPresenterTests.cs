using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class EdgeManagerPresenterTests
{
    [Fact]
    public async Task 새사건은_사용자선택보다_우선해_사진을_전환한다()
    {
        FakeClient client = new();
        FakeView view = new();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        EdgeEntryItem oldEntry = CreateEntry("12가1111", "old.jpg", now);
        client.Entries = new[] { oldEntry };
        EdgeManagerPresenter presenter = new(client, view);
        await presenter.RefreshAsync(CancellationToken.None);
        await presenter.SelectEntryAsync(oldEntry.EventId, CancellationToken.None);

        EdgeEntryItem newEntry = CreateEntry("12가2222", "new.jpg", now.AddSeconds(1));
        client.Entries = new[] { newEntry, oldEntry };
        await presenter.RefreshAsync(CancellationToken.None);

        Assert.Equal("new.jpg", client.LastImageFileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, view.InImage);
    }

    [Fact]
    public async Task 조회실패는_기존목록을_지우지않고_연결상태만_바꾼다()
    {
        FakeClient client = new() { Entries = new[] { CreateEntry("12가3456", "in.jpg", DateTimeOffset.UtcNow) } };
        FakeView view = new();
        EdgeManagerPresenter presenter = new(client, view);
        await presenter.RefreshAsync(CancellationToken.None);
        client.ThrowOnStatus = true;

        await presenter.RefreshAsync(CancellationToken.None);

        Assert.Single(view.Entries);
        Assert.True(view.Disconnected);
    }

    [Fact]
    public async Task 출차행선택은_입차와_출차사진을_함께표시한다()
    {
        FakeClient client = new();
        FakeView view = new();
        EdgeActivityItem activity = new(
            Guid.NewGuid(), EdgeActivityType.Exit, 10, 1, 1, 20, 201,
            "12가3456", DateTimeOffset.UtcNow, "in.jpg", "out.jpg",
            null, null, null, null, true, EdgeDeliveryState.Completed, "OK");
        client.Activities = new[] { activity };
        EdgeManagerPresenter presenter = new(client, view);
        await presenter.RefreshAsync(CancellationToken.None);

        await presenter.SelectActivityAsync(
            activity.ActivityId, activity.ActivityType, CancellationToken.None);

        Assert.Equal(new[] { "in.jpg", "out.jpg" }, client.ImageRequests.TakeLast(2));
        Assert.NotNull(view.InImage);
        Assert.NotNull(view.OutImage);
    }

    [Fact]
    public async Task 사진서버_시간초과는_행선택_예외로_번지지않는다()
    {
        FakeClient client = new();
        FakeView view = new();
        EdgeEntryItem entry = CreateEntry("12가3456", "slow.jpg", DateTimeOffset.UtcNow);
        client.Entries = new[] { entry };
        EdgeManagerPresenter presenter = new(client, view);
        await presenter.RefreshAsync(CancellationToken.None);
        client.ThrowImageTimeout = true;

        Exception? exception = await Record.ExceptionAsync(() =>
            presenter.SelectEntryAsync(entry.EventId, CancellationToken.None));

        Assert.Null(exception);
    }

    private static EdgeEntryItem CreateEntry(
        string carNumber,
        string image,
        DateTimeOffset time) => new(
        Guid.NewGuid(), null, 1, 1, 10, 101, carNumber, time, image,
        EdgeDeliveryState.Completed, "OK");

    private sealed class FakeClient : IEdgeManagementClient
    {
        public IReadOnlyList<EdgeEntryItem> Entries { get; set; } = Array.Empty<EdgeEntryItem>();
        public IReadOnlyList<EdgeActivityItem> Activities { get; set; } = Array.Empty<EdgeActivityItem>();
        public bool ThrowOnStatus { get; set; }
        public bool ThrowImageTimeout { get; set; }
        public string? LastImageFileName { get; private set; }
        public List<string> ImageRequests { get; } = new();

        public Task<EdgeSetupResponse?> GetSetupAsync(CancellationToken cancellationToken) =>
            Task.FromResult<EdgeSetupResponse?>(null);

        public Task<CentralConnectionResponse?> GetCentralConnectionAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<CentralConnectionResponse?>(null);

        public Task<EdgeSetupResponse> SaveSetupAsync(
            EdgeSetupRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new EdgeSetupResponse(
                request.SiteId,
                request.CentralServerUrl,
                request.ImageServerUrl,
                request.ImageWatchPath,
                DateTimeOffset.UtcNow));

        public Task SaveSiteAsync(ParkingSite value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveLaneAsync(ParkingLane value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteLaneAsync(long laneId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveDeviceAsync(ParkingDevice value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteDeviceAsync(long deviceId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveDeviceLinkAsync(ParkingDeviceLink value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteDeviceLinkAsync(long sourceDeviceId, long targetDeviceId, string linkType, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SaveOperationVariableAsync(ParkingOperationVariable value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteOperationVariableAsync(int groupnum, string commandType, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<EdgeServiceStatus> GetStatusAsync(CancellationToken cancellationToken)
        {
            if (ThrowOnStatus) throw new HttpRequestException("offline");
            return Task.FromResult(new EdgeServiceStatus(true, true, true, null, 0));
        }

        public Task<IReadOnlyList<EdgeEntryItem>> GetEntriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Entries);

        public Task<IReadOnlyList<EdgeActivityItem>> GetActivitiesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Activities);

        public Task<SiteConfiguration?> GetConfigurationAsync(CancellationToken cancellationToken) =>
            Task.FromResult<SiteConfiguration?>(null);

        public Task<byte[]?> GetImageAsync(string? fileName, CancellationToken cancellationToken)
        {
            if (ThrowImageTimeout)
                throw new TaskCanceledException("image timeout");
            LastImageFileName = fileName;
            if (fileName is not null) ImageRequests.Add(fileName);
            return Task.FromResult<byte[]?>(fileName is null ? null : new byte[] { 1, 2, 3 });
        }
    }

    private sealed class FakeView : IEdgeManagerView
    {
        public IReadOnlyList<EdgeEntryItem> Entries { get; private set; } = Array.Empty<EdgeEntryItem>();
        public bool Disconnected { get; private set; }
        public byte[]? InImage { get; private set; }
        public byte[]? OutImage { get; private set; }

        public void ShowStatus(EdgeServiceStatus status) => Disconnected = false;
        public void ShowDisconnected() => Disconnected = true;
        public void ShowEntries(IReadOnlyList<EdgeEntryItem> entries) => Entries = entries;
        public void ShowActivities(IReadOnlyList<EdgeActivityItem> activities) { }
        public void ShowConfiguration(SiteConfiguration? configuration) { }
        public void ShowImages(byte[]? inImage, byte[]? outImage)
        {
            InImage = inImage;
            OutImage = outImage;
        }
    }
}
