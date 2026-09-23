using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class ParkingLaneDirectionValidatorTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 차로와_장비의_현장_그룹_방향이_모두_일치해야한다()
    {
        const long siteId = 990001;
        const long laneId = 990010;
        const long deviceId = 990401;
        await using MySqlConnection connection = new(ConnectionString);
        await ClearAsync(connection, siteId);
        try
        {
            await connection.ExecuteAsync("""
                INSERT INTO tparkings(sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
                VALUES (@SiteId,2,'방향시험','TEST',REPEAT('0',64),1);
                INSERT INTO tlaneinfo(laneid,sitenum,groupnum,lanename,direction,useflag)
                VALUES (@LaneId,@SiteId,2,'입차시험','ENTRY',1);
                INSERT INTO tdeviceinfo
                (deviceid,sitenum,groupnum,laneid,devicenum,devicetype,devicename,useflag)
                VALUES (@DeviceId,@SiteId,2,@LaneId,401,3,'입차LPR',1);
                """, new { SiteId = siteId, LaneId = laneId, DeviceId = deviceId });
            ParkingLaneDirectionValidator validator = new(ConnectionString);

            Assert.True(await validator.IsValidAsync(
                siteId, 2, laneId, deviceId, ParkingEventType.Entry, CancellationToken.None));
            Assert.False(await validator.IsValidAsync(
                siteId, 2, laneId, deviceId, ParkingEventType.Exit, CancellationToken.None));
            Assert.False(await validator.IsValidAsync(
                siteId, 1, laneId, deviceId, ParkingEventType.Entry, CancellationToken.None));
        }
        finally
        {
            await ClearAsync(connection, siteId);
        }
    }

    private static Task<int> ClearAsync(MySqlConnection connection, long siteId) =>
        connection.ExecuteAsync("""
            DELETE FROM tparksync WHERE sitenum=@SiteId;
            DELETE FROM tdevicelink WHERE sitenum=@SiteId;
            DELETE FROM tdeviceinfo WHERE sitenum=@SiteId;
            DELETE FROM tlaneinfo WHERE sitenum=@SiteId;
            DELETE FROM tparkings WHERE sitenum=@SiteId;
            """, new { SiteId = siteId });
}
