using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class SiteConfigurationRepositoryTests
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 버전설정의_MySql일시를_UTC로_조회한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        var configuration = await repository.GetVersionedAsync(9001, CancellationToken.None);

        Assert.NotNull(configuration);
        Assert.Equal(TimeSpan.Zero, configuration.UpdatedAtUtc.Offset);
    }

    [Fact]
    public async Task 신규_설정테이블에서_차로_장비_연결정보를_조회한다()
    {
        const long siteId = 990003;
        const long laneId = 990030;
        const long sourceDeviceId = 990431;
        const long targetDeviceId = 990432;
        await using MySqlConnection connection = new(_connectionString);
        await ClearSiteAsync(connection, siteId);
        SiteConfigurationRepository repository = new(_connectionString);
        try
        {
            await connection.ExecuteAsync("""
                INSERT INTO tparkings(sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
                VALUES(@SiteId,2,'설정조회시험','TEST',REPEAT('0',64),1);
                INSERT INTO tlaneinfo(laneid,sitenum,groupnum,lanename,direction,useflag)
                VALUES(@LaneId,@SiteId,2,'입차차로','ENTRY',1);
                INSERT INTO tdeviceinfo
                (deviceid,sitenum,groupnum,laneid,devicenum,devicename,devicetype,useflag)
                VALUES
                (@SourceDeviceId,@SiteId,2,@LaneId,401,'입차LPR',3,1),
                (@TargetDeviceId,@SiteId,2,NULL,201,'출구무인',2,1);
                INSERT INTO tdevicelink
                (sitenum,groupnum,sourcedeviceid,targetdeviceid,linktype,useflag)
                VALUES(@SiteId,2,@SourceDeviceId,@TargetDeviceId,'KIOSK',1);
                """, new { SiteId = siteId, LaneId = laneId, SourceDeviceId = sourceDeviceId, TargetDeviceId = targetDeviceId });

            SiteConfiguration? configuration = await repository.GetAsync(
                siteId, CancellationToken.None);

            Assert.NotNull(configuration);
            Assert.Equal("설정조회시험", configuration.Site.SiteName);
            Assert.Contains(configuration.Lanes, x => x.LaneId == laneId && x.Direction == "ENTRY");
            Assert.Contains(configuration.Devices, x => x.DeviceId == sourceDeviceId && x.DeviceType == "LPR");
            Assert.Contains(configuration.DeviceLinks!, x =>
                x.SourceDeviceId == sourceDeviceId &&
                x.TargetDeviceId == targetDeviceId &&
                x.LinkType == "KIOSK");
        }
        finally
        {
            await ClearSiteAsync(connection, siteId);
        }
    }

    [Fact]
    public async Task 사이트키는_tparkings의_SHA256값으로_검증한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        Assert.True(await repository.ValidateSiteKeyAsync(
            9001,
            "site-9001-key",
            CancellationToken.None));
        Assert.False(await repository.ValidateSiteKeyAsync(
            9001,
            "wrong-key",
            CancellationToken.None));
    }

    [Fact]
    public async Task 존재하지_않는_사이트_수정은_실패한다()
    {
        SiteConfigurationRepository repository = new(_connectionString);

        bool saved = await repository.SaveSiteAsync(
            new ParkingSite(long.MaxValue, "없는 현장", true),
            CancellationToken.None);

        Assert.False(saved);
    }

    [Fact]
    public async Task 버전설정_저장시_변수별_val_opt_규칙을_보존한다()
    {
        const long siteId = 990002;
        const long edgeDeviceId = 990802;
        SiteConfigurationRepository repository = new(_connectionString);
        await using MySqlConnection connection = new(_connectionString);
        await ClearSiteAsync(connection, siteId);
        try
        {
            await connection.ExecuteAsync("""
                INSERT INTO tparkings(sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
                VALUES(@SiteId,2,'변수저장시험','TEST',REPEAT('0',64),1);
                INSERT INTO tdeviceinfo
                (deviceid,sitenum,groupnum,laneid,devicenum,devicename,devicetype,useflag)
                VALUES(@EdgeDeviceId,@SiteId,2,NULL,801,'현장Edge',8,1);
                INSERT INTO tparksync
                (sitenum,deviceid,configversion,appliedversion,status)
                VALUES(@SiteId,@EdgeDeviceId,1,0,'WAIT');
                """, new { SiteId = siteId, EdgeDeviceId = edgeDeviceId });
            VersionedSiteConfiguration value = new(
                new SiteConfiguration(
                    new ParkingSite(siteId, "변수저장시험", true),
                    Array.Empty<ParkingLane>(),
                    new[] { new ParkingDevice(edgeDeviceId, siteId, null, 801, "EDGE", "현장Edge", null, true) },
                    Array.Empty<ParkingDeviceLink>(),
                    new[]
                    {
                        new ParkingOperationVariable(2, "CMD_GRACE_TIME", "30"),
                        new ParkingOperationVariable(2, "CMD_WEEKENDUSE", "1")
                    }),
                2,
                DateTimeOffset.UtcNow);

            bool saved = await repository.SaveVersionedAsync(
                value, CancellationToken.None);

            Assert.True(saved);
            VariableStorageRow optionVariable =
                await connection.QuerySingleAsync<VariableStorageRow>("""
                    SELECT val Value,opt `Option`
                    FROM tparkvariable
                    WHERE sitenum=@SiteId AND groupnum=2 AND cmdtype='CMD_GRACE_TIME';
                    """, new { SiteId = siteId });
            VariableStorageRow valueVariable =
                await connection.QuerySingleAsync<VariableStorageRow>("""
                    SELECT val Value,opt `Option`
                    FROM tparkvariable
                    WHERE sitenum=@SiteId AND groupnum=2 AND cmdtype='CMD_WEEKENDUSE';
                    """, new { SiteId = siteId });
            Assert.Equal("0", optionVariable.Value);
            Assert.Equal("30", optionVariable.Option);
            Assert.Equal("1", valueVariable.Value);
            Assert.Null(valueVariable.Option);
        }
        finally
        {
            await ClearSiteAsync(connection, siteId);
        }
    }

    private static Task<int> ClearSiteAsync(MySqlConnection connection, long siteId) =>
        connection.ExecuteAsync("""
            DELETE FROM tparksync WHERE sitenum=@SiteId;
            DELETE FROM tdevicelink WHERE sitenum=@SiteId;
            DELETE FROM tparkvariable WHERE sitenum=@SiteId;
            DELETE FROM tdeviceinfo WHERE sitenum=@SiteId;
            DELETE FROM tlaneinfo WHERE sitenum=@SiteId;
            DELETE FROM tparkings WHERE sitenum=@SiteId;
            """, new { SiteId = siteId });

    private sealed class VariableStorageRow
    {
        public string? Value { get; set; }
        public string? Option { get; set; }
    }
}
