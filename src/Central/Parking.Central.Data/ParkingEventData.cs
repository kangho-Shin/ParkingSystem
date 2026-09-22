using Dapper;
using MySqlConnector;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.Central.Data;

internal static class ParkingEventData
{
    public static string EventId(Guid value) => value.ToString("N");

    public static Task<int> InsertAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        Guid eventId,
        long siteId,
        int groupnum,
        long laneId,
        long deviceId,
        string eventType,
        string carNumber,
        DateTimeOffset eventAt,
        string? image,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            INSERT IGNORE INTO tparkevent
            (eventid,sitenum,groupnum,laneid,deviceid,devicenum,eventtype,
             carnum,eventdate,image,status)
            SELECT @EventId,@SiteId,@Groupnum,@LaneId,@DeviceId,d.devicenum,
                   UPPER(@EventType),@CarNumber,@EventDate,@Image,'RECEIVED'
            FROM tdeviceinfo d
            WHERE d.deviceid=@DeviceId AND d.sitenum=@SiteId
              AND d.groupnum=@Groupnum AND d.useflag=1
            LIMIT 1;
            """,
            new
            {
                EventId = EventId(eventId),
                SiteId = siteId,
                Groupnum = groupnum,
                LaneId = laneId,
                DeviceId = deviceId,
                EventType = eventType,
                CarNumber = carNumber.Trim(),
                EventDate = ParkingLocalTime.ToDatabase(eventAt),
                Image = VehicleImageName.FileNameOnly(image)
            },
            transaction,
            cancellationToken: cancellationToken));

    public static async Task<FieldEventResponse> ReadResponseAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        string? json = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            "SELECT data FROM tparkevent WHERE eventid=@EventId;",
            new { EventId = EventId(eventId) },
            transaction,
            cancellationToken: cancellationToken));
        return JsonConvert.DeserializeObject<FieldEventResponse>(json ?? "")
            ?? throw new InvalidOperationException("기존 이벤트 처리결과를 읽지 못했습니다.");
    }

    public static Task<int> SaveResponseAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        Guid eventId,
        FieldEventResponse response,
        string parkType,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE tparkevent
            SET data=@ResultData,status='COMPLETE',parktype=@ParkType,
                pindex=@ParkingSessionId,resultcode=@ResultCode,msg=@Message,
                completedate=CURRENT_TIMESTAMP
            WHERE eventid=@EventId;
            """,
            new
            {
                EventId = EventId(eventId),
                ResultData = JsonConvert.SerializeObject(response),
                ParkType = parkType,
                response.ParkingSessionId,
                response.ResultCode,
                Message = response.DisplayMessage
            },
            transaction,
            cancellationToken: cancellationToken));

    public static Task<int> GetDeviceNumberAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        long deviceId,
        CancellationToken cancellationToken) =>
        connection.QuerySingleAsync<int>(new CommandDefinition("""
            SELECT devicenum FROM tdeviceinfo
            WHERE sitenum=@SiteId AND groupnum=@Groupnum AND deviceid=@DeviceId;
            """,
            new { SiteId = siteId, Groupnum = groupnum, DeviceId = deviceId },
            transaction,
            cancellationToken: cancellationToken));

    public static Task IncrementGeneralEntryAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        CancellationToken cancellationToken) =>
        IncrementAsync(connection, transaction, siteId, groupnum, "inilbancnt", cancellationToken);

    public static Task IncrementGeneralExitAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        CancellationToken cancellationToken) =>
        IncrementAsync(connection, transaction, siteId, groupnum, "outilbancnt", cancellationToken);

    public static Task IncrementPeriodEntryAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        CancellationToken cancellationToken) =>
        IncrementAsync(connection, transaction, siteId, groupnum, "inregcnt", cancellationToken);

    public static Task IncrementPeriodExitAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        CancellationToken cancellationToken) =>
        IncrementAsync(connection, transaction, siteId, groupnum, "outregcnt", cancellationToken);

    private static Task IncrementAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        long siteId,
        int groupnum,
        string column,
        CancellationToken cancellationToken)
    {
        string sql = $"""
            INSERT INTO tparkingnum(sitenum,groupnum,{column})
            VALUES(@SiteId,@Groupnum,1)
            ON DUPLICATE KEY UPDATE {column}={column}+1;
            """;
        return connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { SiteId = siteId, Groupnum = groupnum },
            transaction,
            cancellationToken: cancellationToken));
    }
}
