using Newtonsoft.Json;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

internal sealed class ExistingEntrySessionRow
{
    public long ParkingSessionId { get; set; }
    public byte[] EntryEventId { get; set; } = Array.Empty<byte>();
    public DateTime InDateTime { get; set; }
    public string OutFlag { get; set; } = "";
}

public sealed class ParkingEventRepository : IParkingEventRepository
{
    private readonly string _connectionString;

    public ParkingEventRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<FieldEventResponse> SaveEntryAsync(
        FieldEventRequest request,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try {
            byte[] eventId = request.EventId.ToByteArray();

            const string insertEventSql = """
                INSERT IGNORE INTO parking_event
                (eventid, sitenum, groupnum, laneid, deviceid, eventtype,
                 carnum, eventat, imagepath)
                VALUES
                (@EventId, @SiteId, @Groupnum, @LaneId, @DeviceId, @EventType,
                 @CarNumber, @InDateTimeLocal, @InImage);
                """;

            int inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    insertEventSql,
                    new
                    {
                        EventId = eventId,
                        request.SiteId,
                        request.Groupnum,
                        request.LaneId,
                        request.DeviceId,
                        request.EventType,
                        request.CarNumber,
                        InDateTimeLocal = ParkingLocalTime.ToDatabase(request.InDateTime),
                        InImage = VehicleImageName.FileNameOnly(request.InImage)
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (inserted == 0) {
                const string resultSql =
                    "SELECT resultjson FROM parking_event WHERE eventid=@EventId;";

                string resultJson = await connection.QuerySingleAsync<string>(
                    new CommandDefinition(
                        resultSql,
                        new { EventId = eventId },
                        transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return JsonConvert.DeserializeObject<FieldEventResponse>(resultJson)
                       ?? throw new InvalidOperationException("기존 처리결과를 읽지 못했습니다.");
            }

            ExistingEntrySessionRow? existing = await connection.QuerySingleOrDefaultAsync<ExistingEntrySessionRow>(
                new CommandDefinition("""
                    SELECT xindex ParkingSessionId, ineventid EntryEventId,
                           indate InDateTime, outflag OutFlag
                    FROM parking_session
                    WHERE sitenum=@SiteId AND groupnum=@Groupnum
                      AND carnum=@CarNumber AND outflag<>'O'
                    ORDER BY indate DESC LIMIT 1 FOR UPDATE;
                    """,
                    new { request.SiteId, request.Groupnum, request.CarNumber },
                    transaction,
                    cancellationToken: cancellationToken));

            int duplicateEntrySeconds = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition("""
                    SELECT CAST(opt AS SIGNED)
                    FROM tparkvariable
                    WHERE sitenum=@SiteId AND groupnum=@Groupnum
                      AND cmd_type='CMD_DUPLICATE_ENTRY_TIME';
                    """,
                    new { request.SiteId, request.Groupnum },
                    transaction,
                    cancellationToken: cancellationToken)) ?? 10;

            if (existing is not null && existing.OutFlag == "I")
            {
                TimeSpan elapsed = ParkingLocalTime.ToDatabase(request.InDateTime) -
                    existing.InDateTime;

                if (elapsed.TotalSeconds <= Math.Max(duplicateEntrySeconds, 0))
                {
                    FieldEventResponse duplicateResponse = new(
                        request.EventId,
                        true,
                        existing.ParkingSessionId,
                        "ENTRY_DUPLICATE",
                        "이미 입차 처리되었습니다.",
                        true);
                    await SaveResultAsync(
                        connection,
                        transaction,
                        eventId,
                        duplicateResponse,
                        cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return duplicateResponse;
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM parking_session WHERE xindex=@ParkingSessionId;",
                    new { existing.ParkingSessionId },
                    transaction,
                    cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM parking_event WHERE eventid=@EntryEventId;",
                    new { existing.EntryEventId },
                    transaction,
                    cancellationToken: cancellationToken));
            }
            else if (existing is not null && existing.OutFlag == "X")
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE parking_session SET outflag='O'
                    WHERE xindex=@ParkingSessionId;
                    """,
                    new { existing.ParkingSessionId },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            const string insertSessionSql = """
                INSERT INTO parking_session
                (sitenum, ineventid, carnum, groupnum, cartype, inlaneid,
                 indeviceid, indate, inimage, outflag)
                VALUES
                (@SiteId, @EventId, @CarNumber, @Groupnum, 1, @LaneId,
                 @DeviceId, @InDateTimeLocal, @InImage, 'I');

                SELECT LAST_INSERT_ID();
                """;

            long parkingSessionId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    insertSessionSql,
                    new
                    {
                        request.SiteId,
                        EventId = eventId,
                        request.CarNumber,
                        request.Groupnum,
                        request.LaneId,
                        request.DeviceId,
                        InDateTimeLocal = ParkingLocalTime.ToDatabase(request.InDateTime),
                        InImage = VehicleImageName.FileNameOnly(request.InImage)
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            FieldEventResponse response = new(
                request.EventId,
                true,
                parkingSessionId,
                "ENTRY_ACCEPTED",
                "입차되었습니다.",
                true);

            await SaveResultAsync(
                connection,
                transaction,
                eventId,
                response,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Task<int> SaveResultAsync(
        MySqlConnection connection,
        MySqlTransaction transaction,
        byte[] eventId,
        FieldEventResponse response,
        CancellationToken cancellationToken) =>
        connection.ExecuteAsync(new CommandDefinition("""
            UPDATE parking_event
            SET resultjson=@ResultJson
            WHERE eventid=@EventId;
            """,
            new
            {
                EventId = eventId,
                ResultJson = JsonConvert.SerializeObject(response)
            },
            transaction,
            cancellationToken: cancellationToken));
}
