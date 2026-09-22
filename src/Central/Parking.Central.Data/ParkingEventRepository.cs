using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

internal sealed class ExistingEntrySessionRow
{
    public long ParkingSessionId { get; set; }
    public string EntryEventId { get; set; } = "";
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

        try
        {
            int inserted = await ParkingEventData.InsertAsync(
                connection,
                transaction,
                request.EventId,
                request.SiteId,
                request.Groupnum,
                request.LaneId,
                request.DeviceId,
                request.EventType,
                request.CarNumber,
                request.InDateTime,
                request.InImage,
                cancellationToken);

            if (inserted == 0)
            {
                FieldEventResponse previous = await ParkingEventData.ReadResponseAsync(
                    connection, transaction, request.EventId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return previous;
            }

            ExistingEntrySessionRow? existing =
                await connection.QuerySingleOrDefaultAsync<ExistingEntrySessionRow>(
                    new CommandDefinition("""
                        SELECT xindex ParkingSessionId,ineventid EntryEventId,
                               indate InDateTime,outflag OutFlag
                        FROM tparkinfo
                        WHERE sitenum=@SiteId AND groupnum=@Groupnum
                          AND carnum=@CarNumber AND outflag<>'O'
                        ORDER BY indate DESC LIMIT 1 FOR UPDATE;
                        """,
                        new
                        {
                            request.SiteId,
                            request.Groupnum,
                            CarNumber = request.CarNumber.Trim()
                        },
                        transaction,
                        cancellationToken: cancellationToken));

            int duplicateEntrySeconds =
                await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition("""
                    SELECT CAST(opt AS SIGNED)
                    FROM tparkvariable
                    WHERE sitenum=@SiteId AND groupnum=@Groupnum
                      AND cmdtype='CMD_DUPLICATE_ENTRY_TIME' AND useflag=1;
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
                    FieldEventResponse duplicate = new(
                        request.EventId,
                        true,
                        existing.ParkingSessionId,
                        "ENTRY_DUPLICATE",
                        "이미 입차 처리되었습니다.",
                        true);
                    await ParkingEventData.SaveResponseAsync(
                        connection,
                        transaction,
                        request.EventId,
                        duplicate,
                        "GENERAL",
                        cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return duplicate;
                }

                await connection.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM tdiscountinfo WHERE pindex=@ParkingSessionId; " +
                    "DELETE FROM tparkinfo WHERE xindex=@ParkingSessionId;",
                    new { existing.ParkingSessionId },
                    transaction,
                    cancellationToken: cancellationToken));
                await ParkingEventData.IncrementGeneralExitAsync(
                    connection,
                    transaction,
                    request.SiteId,
                    request.Groupnum,
                    cancellationToken);
            }
            else if (existing is not null && existing.OutFlag == "X")
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE tparkinfo
                    SET outflag='O',outdate=@OutDate
                    WHERE xindex=@ParkingSessionId;
                    """,
                    new
                    {
                        existing.ParkingSessionId,
                        OutDate = ParkingLocalTime.ToDatabase(request.InDateTime)
                    },
                    transaction,
                    cancellationToken: cancellationToken));
                await ParkingEventData.IncrementGeneralExitAsync(
                    connection,
                    transaction,
                    request.SiteId,
                    request.Groupnum,
                    cancellationToken);
            }

            int deviceNumber = await ParkingEventData.GetDeviceNumberAsync(
                connection,
                transaction,
                request.SiteId,
                request.Groupnum,
                request.DeviceId,
                cancellationToken);
            long parkingSessionId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition("""
                    INSERT INTO tparkinfo
                    (sitenum,groupnum,ineventid,carnum,cartype,inlaneid,
                     indevicenum,indate,inimage,outflag)
                    VALUES
                    (@SiteId,@Groupnum,@EventId,@CarNumber,1,@LaneId,
                     @DeviceNumber,@InDate,@InImage,'I');
                    SELECT LAST_INSERT_ID();
                    """,
                    new
                    {
                        request.SiteId,
                        request.Groupnum,
                        EventId = ParkingEventData.EventId(request.EventId),
                        CarNumber = request.CarNumber.Trim(),
                        request.LaneId,
                        DeviceNumber = deviceNumber,
                        InDate = ParkingLocalTime.ToDatabase(request.InDateTime),
                        InImage = VehicleImageName.FileNameOnly(request.InImage)
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await ParkingEventData.IncrementGeneralEntryAsync(
                connection,
                transaction,
                request.SiteId,
                request.Groupnum,
                cancellationToken);

            FieldEventResponse response = new(
                request.EventId,
                true,
                parkingSessionId,
                "ENTRY_ACCEPTED",
                "입차되었습니다.",
                true);
            await ParkingEventData.SaveResponseAsync(
                connection,
                transaction,
                request.EventId,
                response,
                "GENERAL",
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
