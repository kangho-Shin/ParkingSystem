using Newtonsoft.Json;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

internal sealed class OpenParkingSessionRow
{
    public long ParkingSessionId { get; set; }
    public long SiteId { get; set; }
    public string CarNumber { get; set; } = "";
    public int Groupnum { get; set; }
    public int CarType { get; set; }
    public long EntryLaneId { get; set; }
    public DateTime EntryAt { get; set; }
    public DateTime? Paydate { get; set; }
    public string Status { get; set; } = "";
}

internal sealed class ExitParkingSessionRow
{
    public long ParkingSessionId { get; set; }
    public string Status { get; set; } = "";
}

public sealed class ParkingExitRepository : IParkingExitRepository
{
    private readonly string _connectionString;
    public ParkingExitRepository(string connectionString) { _connectionString = connectionString; }

    public async Task<OpenParkingSessionResponse?> FindOpenAsync(long siteId, string carNumber, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT xindex ParkingSessionId, sitenum SiteId, carnum CarNumber,
                   groupnum Groupnum, cartype CarType, inlaneid EntryLaneId,
                   indate EntryAt, paydate Paydate, outflag Status
            FROM parking_session
            WHERE sitenum=@SiteId AND carnum=@CarNumber AND outflag<>'O'
            ORDER BY indate DESC LIMIT 1;
            """;
        await using MySqlConnection connection = new(_connectionString);
        OpenParkingSessionRow? row = await connection.QuerySingleOrDefaultAsync<OpenParkingSessionRow>(
            new CommandDefinition(
                sql,
                new { SiteId = siteId, CarNumber = carNumber },
                cancellationToken: cancellationToken));

        return ToResponse(row);
    }

    public async Task<OpenParkingSessionResponse?> FindOpenByIdAsync(
        long parkingSessionId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT xindex ParkingSessionId, sitenum SiteId, carnum CarNumber,
                   groupnum Groupnum, cartype CarType, inlaneid EntryLaneId,
                   indate EntryAt, paydate Paydate, outflag Status
            FROM parking_session
            WHERE xindex=@ParkingSessionId AND outflag<>'O';
            """;
        await using MySqlConnection connection = new(_connectionString);
        OpenParkingSessionRow? row = await connection.QuerySingleOrDefaultAsync<OpenParkingSessionRow>(
            new CommandDefinition(
                sql,
                new { ParkingSessionId = parkingSessionId },
                cancellationToken: cancellationToken));

        return ToResponse(row);
    }

    public async Task MarkSettledAsync(
        long parkingSessionId,
        DateTimeOffset settledAt,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE parking_session
            SET outflag='X', paydate=@SettledAtUtc
            WHERE xindex=@ParkingSessionId AND outflag='I';
            """,
            new
            {
                ParkingSessionId = parkingSessionId,
                SettledAtUtc = settledAt.UtcDateTime
            },
            cancellationToken: cancellationToken));
    }

    private static OpenParkingSessionResponse? ToResponse(OpenParkingSessionRow? row)
    {
        if (row is null)
            return null;

        DateTime entryAtUtc = DateTime.SpecifyKind(row.EntryAt, DateTimeKind.Utc);
        DateTimeOffset? paydate = row.Paydate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(row.Paydate.Value, DateTimeKind.Utc))
            : null;
        return new OpenParkingSessionResponse(
            row.ParkingSessionId,
            row.SiteId,
            row.CarNumber,
            row.Groupnum,
            row.CarType,
            row.EntryLaneId,
            new DateTimeOffset(entryAtUtc),
            paydate,
            row.Status);
    }

    public async Task<FieldEventResponse> SaveExitAsync(
        ExitEventRequest request,
        bool exitAllowed,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        byte[] eventId = request.EventId.ToByteArray();

        try
        {
            const string insertEvent = """
                INSERT IGNORE INTO parking_event
                (eventid,sitenum,groupnum,laneid,deviceid,eventtype,carnum,eventat,imagepath)
                VALUES (@EventId,@SiteId,@Groupnum,@LaneId,@DeviceId,@EventType,
                        @CarNumber,@OutDateTimeUtc,@OutImage);
                """;
            int inserted = await connection.ExecuteAsync(new CommandDefinition(insertEvent, new
            {
                EventId = eventId, request.SiteId, request.Groupnum,
                request.LaneId, request.DeviceId, request.EventType,
                request.CarNumber, OutDateTimeUtc = request.OutDateTime.UtcDateTime,
                OutImage = VehicleImageName.FileNameOnly(request.OutImage)
            }, transaction, cancellationToken: cancellationToken));

            if (inserted == 0)
            {
                string json = await connection.QuerySingleAsync<string>(new CommandDefinition(
                    "SELECT resultjson FROM parking_event WHERE eventid=@EventId;",
                    new { EventId = eventId }, transaction, cancellationToken: cancellationToken));
                await transaction.CommitAsync(cancellationToken);
                return JsonConvert.DeserializeObject<FieldEventResponse>(json)
                    ?? throw new InvalidOperationException("기존 출차결과를 읽지 못했습니다.");
            }

            ExitParkingSessionRow? session = await connection.QuerySingleOrDefaultAsync<ExitParkingSessionRow>(new CommandDefinition("""
                SELECT xindex ParkingSessionId, outflag Status
                FROM parking_session
                WHERE sitenum=@SiteId AND carnum=@CarNumber AND outflag<>'O'
                ORDER BY indate DESC LIMIT 1 FOR UPDATE;
                """, new { request.SiteId, request.CarNumber }, transaction, cancellationToken: cancellationToken));

            FieldEventResponse result;
            if (session is null)
            {
                result = new FieldEventResponse(request.EventId, false, null, "OPEN_SESSION_NOT_FOUND", "미출차 차량이 없습니다.", false);
            }
            else if (!exitAllowed)
            {
                result = new FieldEventResponse(
                    request.EventId,
                    false,
                    session.ParkingSessionId,
                    "PAYMENT_REQUIRED",
                    "결제가 필요합니다.",
                    false);
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE parking_session SET outeventid=@EventId, outlaneid=@LaneId,
                    outdeviceid=@DeviceId, outdate=@OutDateTimeUtc,
                    outimage=@OutImage, outflag='O'
                    WHERE xindex=@ParkingSessionId;
                    """, new
                    {
                        EventId = eventId,
                        request.LaneId,
                        request.DeviceId,
                        OutDateTimeUtc = request.OutDateTime.UtcDateTime,
                        OutImage = VehicleImageName.FileNameOnly(request.OutImage),
                        session.ParkingSessionId
                    },
                    transaction, cancellationToken: cancellationToken));
                result = new FieldEventResponse(request.EventId, true, session.ParkingSessionId, "EXIT_ACCEPTED", "출차되었습니다.", true);
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE parking_event SET resultjson=@ResultJson WHERE eventid=@EventId;",
                new { EventId = eventId, ResultJson = JsonConvert.SerializeObject(result) },
                transaction, cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
