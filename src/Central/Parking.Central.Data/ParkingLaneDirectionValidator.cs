using Dapper;
using MySqlConnector;

namespace Parking.Central.Data;

public sealed class ParkingLaneDirectionValidator : IParkingLaneDirectionValidator
{
    private readonly string _connectionString;

    public ParkingLaneDirectionValidator(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<bool> IsValidAsync(
        long siteId,
        int groupnum,
        long laneId,
        long deviceId,
        string eventType,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            SELECT COUNT(*)
            FROM tlaneinfo l
            JOIN tdeviceinfo d
              ON d.sitenum=l.sitenum AND d.groupnum=l.groupnum AND d.laneid=l.laneid
            WHERE l.sitenum=@SiteId AND l.groupnum=@Groupnum
              AND l.laneid=@LaneId AND d.deviceid=@DeviceId
              AND l.useflag=1 AND d.useflag=1
              AND (UPPER(l.direction)=UPPER(@EventType) OR UPPER(l.direction)='BOTH');
            """,
            new { SiteId = siteId, Groupnum = groupnum, LaneId = laneId, DeviceId = deviceId, EventType = eventType },
            cancellationToken: cancellationToken));
        return count == 1;
    }
}
