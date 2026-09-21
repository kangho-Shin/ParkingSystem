using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeManagementStoreTests
{
    [Fact]
    public async Task 로컬설정이_없으면_중앙버전0도_적용한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"parking-edge-initial-sync-{Guid.NewGuid():N}.db");

        try
        {
            LocalConfigurationStore store = new(
                $"Data Source={databasePath};Pooling=False");
            await store.InitializeAsync(CancellationToken.None);
            SiteConfiguration configuration = new(
                new ParkingSite(9001, "시험현장", true),
                Array.Empty<ParkingLane>(),
                Array.Empty<ParkingDevice>());

            await store.ApplyRemoteAsync(
                new VersionedSiteConfiguration(
                    configuration, 0, DateTimeOffset.UtcNow),
                CancellationToken.None);

            SiteConfiguration? saved = await store.GetAsync(
                9001, CancellationToken.None);
            Assert.NotNull(saved);
            Assert.Equal(9001, saved.Site.SiteId);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task SQLite설정을_계약자료형으로_변환해_조회한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"parking-edge-types-{Guid.NewGuid():N}.db");

        try
        {
            LocalConfigurationStore store = new(
                $"Data Source={databasePath};Pooling=False");
            await store.InitializeAsync(CancellationToken.None);
            await store.SaveAsync(new SiteConfiguration(
                new ParkingSite(9001, "시험현장", true),
                new[] { new ParkingLane(9010, 9001, 2, "입차", "Entry", true) },
                new[] { new ParkingDevice(4001, 9001, 9010, 401, "LPR", "입차LPR", null, true, 29200) },
                new[] { new ParkingDeviceLink(9001, 4001, 4001, "LPR", true) },
                new[] { new ParkingOperationVariable(2, "TEST", "1") }),
                CancellationToken.None);

            SiteConfiguration? saved = await store.GetAsync(
                9001, CancellationToken.None);

            Assert.NotNull(saved);
            Assert.True(saved.Site.Enabled);
            Assert.Equal(2, saved.Lanes.Single().GroupNumber);
            Assert.True(saved.Lanes.Single().Enabled);
            Assert.Equal(401, saved.Devices.Single().DeviceNumber);
            Assert.Equal(29200, saved.Devices.Single().Port);
            Assert.True(saved.Devices.Single().Enabled);
            Assert.True(saved.DeviceLinks!.Single().Enabled);
            Assert.Equal(2, saved.OperationVariables!.Single().Groupnum);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

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
