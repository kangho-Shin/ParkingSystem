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
            FROM tparkinfo
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
            FROM tparkinfo
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
            UPDATE tparkinfo
            SET outflag='X', paydate=@SettledAtLocal
            WHERE xindex=@ParkingSessionId AND outflag='I';
            """,
            new
            {
                ParkingSessionId = parkingSessionId,
                SettledAtLocal = ParkingLocalTime.ToDatabase(settledAt)
            },
            cancellationToken: cancellationToken));
    }

    public async Task SaveCalculationAsync(
        long parkingSessionId,
        int parkingMinutes,
        long originalFee,
        long discountFee,
        long payFee,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE tparkinfo
            SET parktime=@ParkingMinutes,parkfee=@OriginalFee,
                discountfee=@DiscountFee,payfee=@PayFee
            WHERE xindex=@ParkingSessionId AND outflag<>'O';
            """,
            new { ParkingSessionId = parkingSessionId, parkingMinutes, originalFee, discountFee, payFee },
            cancellationToken: cancellationToken));
    }

    private static OpenParkingSessionResponse? ToResponse(OpenParkingSessionRow? row)
    {
        if (row is null)
            return null;

        DateTimeOffset? paydate = row.Paydate.HasValue
            ? ParkingLocalTime.FromDatabase(row.Paydate.Value)
            : null;
        return new OpenParkingSessionResponse(
            row.ParkingSessionId,
            row.SiteId,
            row.CarNumber,
            row.Groupnum,
            row.CarType,
            row.EntryLaneId,
            ParkingLocalTime.FromDatabase(row.EntryAt),
            paydate,
            row.Status);
    }

    public async Task<FieldEventResponse> SaveExitAsync(
        ExitEventRequest request,
        bool exitAllowed,
        CancellationToken cancellationToken,
        string acceptedResultCode = "EXIT_ACCEPTED")
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            int inserted = await ParkingEventData.InsertAsync(
                connection, transaction, request.EventId, request.SiteId,
                request.Groupnum, request.LaneId, request.DeviceId,
                request.EventType, request.CarNumber, request.OutDateTime,
                request.OutImage, cancellationToken);

            if (inserted == 0)
            {
                FieldEventResponse previous = await ParkingEventData.ReadResponseAsync(
                    connection, transaction, request.EventId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return previous;
            }

            ExitParkingSessionRow? session = await connection.QuerySingleOrDefaultAsync<ExitParkingSessionRow>(new CommandDefinition("""
                SELECT xindex ParkingSessionId, outflag Status
                FROM tparkinfo
                WHERE sitenum=@SiteId AND groupnum=@Groupnum
                  AND carnum=@CarNumber AND outflag<>'O'
                ORDER BY indate DESC LIMIT 1 FOR UPDATE;
                """, new
                {
                    request.SiteId,
                    request.Groupnum,
                    CarNumber = request.CarNumber.Trim()
                }, transaction, cancellationToken: cancellationToken));

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
                int deviceNumber = await ParkingEventData.GetDeviceNumberAsync(
                    connection, transaction, request.SiteId, request.Groupnum,
                    request.DeviceId, cancellationToken);
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE tparkinfo SET outeventid=@EventId,outlaneid=@LaneId,
                    outdevicenum=@DeviceNumber,outdate=@OutDate,
                    outimage=@OutImage,
                    parktime=GREATEST(TIMESTAMPDIFF(MINUTE,indate,@OutDate),0),
                    outflag='O'
                    WHERE xindex=@ParkingSessionId;
                    """, new
                    {
                        EventId = ParkingEventData.EventId(request.EventId),
                        request.LaneId,
                        DeviceNumber = deviceNumber,
                        OutDate = ParkingLocalTime.ToDatabase(request.OutDateTime),
                        OutImage = VehicleImageName.FileNameOnly(request.OutImage),
                        session.ParkingSessionId
                    },
                    transaction, cancellationToken: cancellationToken));
                await ParkingEventData.IncrementGeneralExitAsync(
                    connection, transaction, request.SiteId, request.Groupnum,
                    cancellationToken);
                result = new FieldEventResponse(
                    request.EventId, true, session.ParkingSessionId,
                    acceptedResultCode,
                    acceptedResultCode == "KIOSK_OFFLINE_OPEN"
                        ? "무인 연결 장애로 출차합니다."
                        : "출차되었습니다.",
                    true);
            }

            await ParkingEventData.SaveResponseAsync(
                connection, transaction, request.EventId, result, "GENERAL",
                cancellationToken);
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
