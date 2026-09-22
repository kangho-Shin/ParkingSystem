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
        const long siteId = 9001;
        const long laneId = 9010;
        const long deviceId = 4001;
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO parking_site(sitenum,sitename,useflag)
            VALUES (@SiteId,'방향시험',1)
            ON DUPLICATE KEY UPDATE useflag=1;
            INSERT INTO parking_lane(laneid,sitenum,groupnum,lanename,direction,useflag)
            VALUES (@LaneId,@SiteId,2,'입차시험','Entry',1)
            ON DUPLICATE KEY UPDATE groupnum=2,direction='Entry',useflag=1;
            INSERT INTO parking_device
            (deviceid,sitenum,laneid,devicenum,devicetype,devicename,useflag)
            VALUES (@DeviceId,@SiteId,@LaneId,401,'LPR','입차LPR',1)
            ON DUPLICATE KEY UPDATE sitenum=@SiteId,laneid=@LaneId,useflag=1;
            """, new { SiteId = siteId, LaneId = laneId, DeviceId = deviceId });
        ParkingLaneDirectionValidator validator = new(ConnectionString);

        Assert.True(await validator.IsValidAsync(
            siteId, 2, laneId, deviceId, ParkingEventType.Entry, CancellationToken.None));
        Assert.False(await validator.IsValidAsync(
            siteId, 2, laneId, deviceId, ParkingEventType.Exit, CancellationToken.None));
        Assert.False(await validator.IsValidAsync(
            siteId, 1, laneId, deviceId, ParkingEventType.Entry, CancellationToken.None));
    }
}
