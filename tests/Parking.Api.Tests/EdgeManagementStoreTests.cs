using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeManagementStoreTests
{
    [Fact]
    public async Task 설정동기화시각과_대기Outbox수를_조회한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"parking-edge-management-{Guid.NewGuid():N}.db");

        try
        {
            string connectionString = $"Data Source={databasePath};Pooling=False";
            LocalConfigurationStore configurationStore = new(connectionString);
            SqliteOutboxRepository outboxRepository = new(connectionString);
            await configurationStore.InitializeAsync(CancellationToken.None);
            await outboxRepository.InitializeAsync(CancellationToken.None);

            SiteConfiguration configuration = new(
                new ParkingSite(1, "시험현장", true),
                Array.Empty<ParkingLane>(),
                Array.Empty<ParkingDevice>());
            FieldEventRequest entry = new(
                Guid.NewGuid(), 1, 10, 101, "12가3456", DateTimeOffset.UtcNow);

            await configurationStore.SaveAsync(configuration, CancellationToken.None);
            await outboxRepository.EnqueueEntryAsync(entry, CancellationToken.None);

            Assert.NotNull(await configurationStore.GetSyncedAtAsync(CancellationToken.None));
            Assert.Equal(1, await outboxRepository.CountPendingAsync(CancellationToken.None));
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Fact]
    public void 관리계약은_입차와_정산출차를_구분한다()
    {
        EdgeActivityItem item = new(
            Guid.NewGuid(),
            EdgeActivityType.Payment,
            10,
            1,
            1,
            null,
            null,
            "12가3456",
            DateTimeOffset.UtcNow,
            "in.jpg",
            null,
            1000,
            200,
            800,
            "Card",
            null,
            EdgeDeliveryState.Completed,
            "OK");

        Assert.Equal(EdgeActivityType.Payment, item.ActivityType);
    }
}
