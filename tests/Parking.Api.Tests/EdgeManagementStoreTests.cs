using Dapper;
using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeManagementStoreTests
{
    [Fact]
    public async Task 기존_로컬설정DB에_dirty상태를_추가한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"parking-edge-dirty-migration-{Guid.NewGuid():N}.db");

        try
        {
            string connectionString = $"Data Source={databasePath};Pooling=False";
            await using (Microsoft.Data.Sqlite.SqliteConnection connection = new(connectionString))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync("""
                    CREATE TABLE local_configuration_state(
                        site_id INTEGER PRIMARY KEY,
                        version INTEGER NOT NULL,
                        updated_at_utc TEXT NOT NULL);
                    """);
            }

            LocalConfigurationStore store = new(connectionString);
            await store.InitializeAsync(CancellationToken.None);

            await using Microsoft.Data.Sqlite.SqliteConnection verify = new(connectionString);
            IReadOnlyList<string> columns = (await verify.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('local_configuration_state');")).AsList();
            Assert.Contains("dirty", columns);
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task 로컬설정_수정은_동기화완료때까지_dirty로_유지한다()
    {
        string databasePath = Path.Combine(
            Path.GetTempPath(),
            $"parking-edge-dirty-state-{Guid.NewGuid():N}.db");

        try
        {
            LocalConfigurationStore store = new(
                $"Data Source={databasePath};Pooling=False");
            await store.InitializeAsync(CancellationToken.None);
            SiteConfiguration configuration = new(
                new ParkingSite(9001, "시험현장", true),
                Array.Empty<ParkingLane>(),
                new[] { new ParkingDevice(4001, 9001, null, 401, "LPR", "깨진장치명", null, true, 29200) });
            await store.ApplyRemoteAsync(
                new VersionedSiteConfiguration(configuration, 10, DateTimeOffset.UtcNow),
                CancellationToken.None);
            Assert.False(await store.IsDirtyAsync(9001, CancellationToken.None));

            await store.SaveDeviceAsync(
                configuration.Devices.Single() with { DeviceName = "입차LPR" },
                CancellationToken.None);
            Assert.True(await store.IsDirtyAsync(9001, CancellationToken.None));

            await store.SetStateAsync(9001, 11, DateTimeOffset.UtcNow, CancellationToken.None);
            Assert.False(await store.IsDirtyAsync(9001, CancellationToken.None));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Theory]
    [InlineData(true, 2, 10, ConfigurationSyncAction.PushLocal)]
    [InlineData(false, 2, 10, ConfigurationSyncAction.PullRemote)]
    [InlineData(false, 10, 2, ConfigurationSyncAction.PushLocal)]
    [InlineData(false, 10, 10, ConfigurationSyncAction.None)]
    public void 동기화방향은_시간이아닌_버전과_dirty상태로_결정한다(
        bool localDirty,
        long localVersion,
        long remoteVersion,
        ConfigurationSyncAction expected)
    {
        ConfigurationSyncAction result = ConfigurationSyncPolicy.Resolve(
            localDirty, localVersion, remoteVersion);

        Assert.Equal(expected, result);
    }

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
