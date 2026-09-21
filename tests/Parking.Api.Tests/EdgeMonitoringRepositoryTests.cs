using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeMonitoringRepositoryTests
{
    [Fact]
    public async Task 입차는_중복없이_저장되고_허용출차때만_삭제된다()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        FieldEventRequest entry = CreateEntry("12가3456");

        await database.Repository.RecordEntryAsync(entry, CancellationToken.None);
        await database.Repository.RecordEntryAsync(entry, CancellationToken.None);
        Assert.Single(await database.Repository.GetEntriesAsync(1000, CancellationToken.None));

        ExitEventRequest blockedExit = CreateExit("12가3456");
        await database.Repository.RecordExitAsync(
            blockedExit,
            new FieldEventResponse(blockedExit.EventId, false, null, "UNPAID", "미결제", false),
            EdgeDeliveryState.Completed,
            CancellationToken.None);
        Assert.Single(await database.Repository.GetEntriesAsync(1000, CancellationToken.None));

        ExitEventRequest allowedExit = CreateExit("12가3456");
        await database.Repository.RecordExitAsync(
            allowedExit,
            new FieldEventResponse(allowedExit.EventId, true, null, "OK", "출차", true),
            EdgeDeliveryState.Completed,
            CancellationToken.None);
        Assert.Empty(await database.Repository.GetEntriesAsync(1000, CancellationToken.None));
    }

    [Fact]
    public async Task 정산과_출차는_각각_처리목록에_남는다()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        FieldEventRequest entry = CreateEntry("12가3456");
        await database.Repository.RecordEntryAsync(entry, CancellationToken.None);
        await database.Repository.CompleteEntryDeliveryAsync(
            entry.EventId,
            new FieldEventResponse(entry.EventId, true, 10, "OK", "입차", true),
            EdgeDeliveryState.Completed,
            CancellationToken.None);

        CompletePaymentRequest payment = CreatePayment(10);
        await database.Repository.RecordPaymentAsync(
            payment, EdgeDeliveryState.Completed, "OK", CancellationToken.None);
        ExitEventRequest exit = CreateExit("12가3456");
        await database.Repository.RecordExitAsync(
            exit,
            new FieldEventResponse(exit.EventId, true, 10, "OK", "출차", true),
            EdgeDeliveryState.Completed,
            CancellationToken.None);

        IReadOnlyList<EdgeActivityItem> activities =
            await database.Repository.GetActivitiesAsync(1000, CancellationToken.None);
        Assert.Equal(2, activities.Count);
        Assert.Contains(activities, x => x.ActivityType == EdgeActivityType.Payment);
        Assert.Contains(activities, x => x.ActivityType == EdgeActivityType.Exit);
    }

    [Fact]
    public async Task 같은식별자는_처리목록에_중복되지않는다()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        CompletePaymentRequest payment = CreatePayment(999);

        await database.Repository.RecordPaymentAsync(
            payment, EdgeDeliveryState.Pending, "PENDING", CancellationToken.None);
        await database.Repository.RecordPaymentAsync(
            payment, EdgeDeliveryState.Completed, "OK", CancellationToken.None);

        EdgeActivityItem activity = Assert.Single(
            await database.Repository.GetActivitiesAsync(1000, CancellationToken.None));
        Assert.Equal(EdgeDeliveryState.Completed, activity.DeliveryState);
        Assert.Equal("", activity.CarNumber);
    }

    [Fact]
    public async Task 처리목록은_최신1000건만_유지한다()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        DateTimeOffset start = new(2026, 9, 19, 0, 0, 0, TimeSpan.Zero);

        for (int index = 0; index < 1001; index++)
        {
            CompletePaymentRequest payment = CreatePayment(index + 1);
            payment.PaidAt = start.AddSeconds(index);
            await database.Repository.RecordPaymentAsync(
                payment, EdgeDeliveryState.Completed, "OK", CancellationToken.None);
        }

        IReadOnlyList<EdgeActivityItem> activities =
            await database.Repository.GetActivitiesAsync(1000, CancellationToken.None);
        Assert.Equal(1000, activities.Count);
        Assert.Equal(start.AddSeconds(1000), activities[0].OccurredAt);
        Assert.DoesNotContain(activities, x => x.OccurredAt == start);
    }

    private static FieldEventRequest CreateEntry(string carNumber) => new(
        Guid.NewGuid(), 1, 10, 101, carNumber, DateTimeOffset.UtcNow,
        Groupnum: 1, InImage: "in.jpg");

    private static ExitEventRequest CreateExit(string carNumber) => new(
        Guid.NewGuid(), 1, 20, 201, carNumber, DateTimeOffset.UtcNow,
        Groupnum: 1, OutImage: "out.jpg");

    private static CompletePaymentRequest CreatePayment(long parkingSessionId) => new()
    {
        PaymentId = Guid.NewGuid(),
        ParkingSessionId = parkingSessionId,
        SiteId = 1,
        OriginalFee = 1000,
        DiscountFee = 200,
        PaidAmount = 800,
        PaymentMethod = "Card",
        PaidAt = DateTimeOffset.UtcNow
    };

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string path, EdgeMonitoringRepository repository)
        {
            Path = path;
            Repository = repository;
        }

        private string Path { get; }
        public EdgeMonitoringRepository Repository { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"parking-monitor-{Guid.NewGuid():N}.db");
            EdgeMonitoringRepository repository = new($"Data Source={path};Pooling=False");
            await repository.InitializeAsync(CancellationToken.None);
            return new TestDatabase(path, repository);
        }

        public ValueTask DisposeAsync()
        {
            if (File.Exists(Path))
                File.Delete(Path);
            return ValueTask.CompletedTask;
        }
    }
}
