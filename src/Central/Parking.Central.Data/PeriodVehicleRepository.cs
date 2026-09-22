using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class PeriodVehicleRepository : IPeriodVehicleRepository
{
    private readonly string _connectionString;

    public PeriodVehicleRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<PeriodMember?> FindMemberAsync(
        long siteId,
        int groupnum,
        string carNumber,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        PeriodMemberRow? row = await connection.QueryFirstOrDefaultAsync<PeriodMemberRow>(
            new CommandDefinition("""
                SELECT xindex MemberId,sitenum SiteId,@Groupnum Groupnum,
                       cardno CardNumber,name Name,carnum1 CarNumber,
                       cartype1 CarType,enddate EndDate
                FROM tperiodmember
                WHERE sitenum=@SiteId AND useflag=1 AND carnum1=@CarNumber
                  AND startdate<=@CheckDate AND enddate>=@CheckDate
                  AND (groupnum=@Groupnum OR SUBSTRING(parkarea,@Groupnum,1)='1')
                ORDER BY enddate DESC,xindex DESC
                LIMIT 1;
                """,
                new
                {
                    SiteId = siteId,
                    Groupnum = groupnum,
                    CarNumber = carNumber.Trim(),
                    CheckDate = at.Date
                },
                cancellationToken: cancellationToken));
        return row is null
            ? null
            : new PeriodMember(
                row.MemberId,
                row.SiteId,
                checked((int)row.Groupnum),
                row.CardNumber,
                row.Name,
                row.CarNumber,
                row.CarType,
                row.EndDate);
    }

    public async Task<FieldEventResponse> SaveEntryAsync(
        FieldEventRequest request,
        PeriodMember member,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            int inserted = await ParkingEventData.InsertAsync(
                connection, transaction, request.EventId, request.SiteId,
                request.Groupnum, request.LaneId, request.DeviceId,
                request.EventType, request.CarNumber, request.InDateTime,
                request.InImage, cancellationToken);
            if (inserted == 0)
            {
                FieldEventResponse previous = await ParkingEventData.ReadResponseAsync(
                    connection, transaction, request.EventId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return previous;
            }

            int deviceNumber = await ParkingEventData.GetDeviceNumberAsync(
                connection, transaction, request.SiteId, request.Groupnum,
                request.DeviceId, cancellationToken);
            long? existingId = await connection.QuerySingleOrDefaultAsync<long?>(
                new CommandDefinition("""
                    SELECT xindex FROM tperiodinout
                    WHERE sitenum=@SiteId AND groupnum=@Groupnum
                      AND carnum=@CarNumber AND outflag='I'
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

            if (existingId.HasValue)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE tperiodinout
                    SET outdate=@OutDate,outlaneid=@LaneId,
                        outdevicenum=@DeviceNumber,outimage=@OutImage,
                        parktime=GREATEST(TIMESTAMPDIFF(MINUTE,indate,@OutDate),0),
                        outflag='O',note='DUPLICATE_ENTRY'
                    WHERE xindex=@PeriodSessionId;
                    """,
                    new
                    {
                        PeriodSessionId = existingId.Value,
                        OutDate = ParkingLocalTime.ToDatabase(request.InDateTime),
                        request.LaneId,
                        DeviceNumber = deviceNumber,
                        OutImage = VehicleImageName.FileNameOnly(request.InImage)
                    },
                    transaction,
                    cancellationToken: cancellationToken));
                await ParkingEventData.IncrementPeriodExitAsync(
                    connection, transaction, request.SiteId, request.Groupnum,
                    cancellationToken);
            }

            long periodSessionId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition("""
                    INSERT INTO tperiodinout
                    (periodindex,sitenum,groupnum,ineventid,cardno,name,enddate,
                     carnum,cartype,inlaneid,indevicenum,indate,inimage,outflag)
                    VALUES
                    (@MemberId,@SiteId,@Groupnum,@EventId,@CardNumber,@Name,@EndDate,
                     @CarNumber,@CarType,@LaneId,@DeviceNumber,@InDate,@InImage,'I');
                    SELECT LAST_INSERT_ID();
                    """,
                    new
                    {
                        member.MemberId,
                        request.SiteId,
                        request.Groupnum,
                        EventId = ParkingEventData.EventId(request.EventId),
                        member.CardNumber,
                        member.Name,
                        member.EndDate,
                        CarNumber = request.CarNumber.Trim(),
                        member.CarType,
                        request.LaneId,
                        DeviceNumber = deviceNumber,
                        InDate = ParkingLocalTime.ToDatabase(request.InDateTime),
                        InImage = VehicleImageName.FileNameOnly(request.InImage)
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE tperiodmember SET outflag='I' WHERE xindex=@MemberId;",
                new { member.MemberId },
                transaction,
                cancellationToken: cancellationToken));
            await ParkingEventData.IncrementPeriodEntryAsync(
                connection, transaction, request.SiteId, request.Groupnum,
                cancellationToken);

            FieldEventResponse response = new(
                request.EventId,
                true,
                periodSessionId,
                existingId.HasValue ? "PERIOD_ENTRY_DUPLICATE" : "PERIOD_ENTRY_ACCEPTED",
                "등록차량 입차가 처리되었습니다.",
                true);
            await ParkingEventData.SaveResponseAsync(
                connection, transaction, request.EventId, response, "PERIOD",
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

    public async Task<OpenPeriodSession?> FindOpenAsync(
        long siteId,
        int groupnum,
        string carNumber,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        PeriodSessionRow? row = await connection.QueryFirstOrDefaultAsync<PeriodSessionRow>(
            new CommandDefinition("""
                SELECT xindex PeriodSessionId,periodindex MemberId,sitenum SiteId,
                       groupnum Groupnum,carnum CarNumber,indate InDateTime,
                       inimage InImage,outflag OutFlag
                FROM tperiodinout
                WHERE sitenum=@SiteId AND groupnum=@Groupnum
                  AND carnum=@CarNumber AND outflag='I'
                ORDER BY indate DESC LIMIT 1;
                """,
                new
                {
                    SiteId = siteId,
                    Groupnum = groupnum,
                    CarNumber = carNumber.Trim()
                },
                cancellationToken: cancellationToken));
        return row is null
            ? null
            : new OpenPeriodSession(
                row.PeriodSessionId,
                row.MemberId,
                row.SiteId,
                row.Groupnum,
                row.CarNumber,
                ParkingLocalTime.FromDatabase(row.InDateTime),
                row.InImage,
                row.OutFlag);
    }

    public async Task<FieldEventResponse> SaveExitAsync(
        ExitEventRequest request,
        OpenPeriodSession session,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

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

            int deviceNumber = await ParkingEventData.GetDeviceNumberAsync(
                connection, transaction, request.SiteId, request.Groupnum,
                request.DeviceId, cancellationToken);
            int affected = await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE tperiodinout
                SET outeventid=@EventId,outlaneid=@LaneId,
                    outdevicenum=@DeviceNumber,outdate=@OutDate,outimage=@OutImage,
                    parktime=GREATEST(TIMESTAMPDIFF(MINUTE,indate,@OutDate),0),
                    outflag='O'
                WHERE xindex=@PeriodSessionId AND outflag='I';
                """,
                new
                {
                    EventId = ParkingEventData.EventId(request.EventId),
                    request.LaneId,
                    DeviceNumber = deviceNumber,
                    OutDate = ParkingLocalTime.ToDatabase(request.OutDateTime),
                    OutImage = VehicleImageName.FileNameOnly(request.OutImage),
                    session.PeriodSessionId
                },
                transaction,
                cancellationToken: cancellationToken));
            if (affected == 0)
            {
                FieldEventResponse rejected = new(
                    request.EventId,
                    false,
                    session.PeriodSessionId,
                    "PERIOD_SESSION_NOT_OPEN",
                    "이미 출차된 등록차량입니다.",
                    false);
                await ParkingEventData.SaveResponseAsync(
                    connection, transaction, request.EventId, rejected, "PERIOD",
                    cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return rejected;
            }
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE tperiodmember SET outflag='O' WHERE xindex=@MemberId;",
                new { session.MemberId },
                transaction,
                cancellationToken: cancellationToken));
            await ParkingEventData.IncrementPeriodExitAsync(
                connection, transaction, request.SiteId, request.Groupnum,
                cancellationToken);

            FieldEventResponse response = new(
                request.EventId,
                true,
                session.PeriodSessionId,
                "PERIOD_EXIT_ACCEPTED",
                "등록차량 출차가 처리되었습니다.",
                true);
            await ParkingEventData.SaveResponseAsync(
                connection, transaction, request.EventId, response, "PERIOD",
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

    private sealed class PeriodSessionRow
    {
        public long PeriodSessionId { get; set; }
        public long MemberId { get; set; }
        public long SiteId { get; set; }
        public int Groupnum { get; set; }
        public string CarNumber { get; set; } = "";
        public DateTime InDateTime { get; set; }
        public string? InImage { get; set; }
        public string OutFlag { get; set; } = "";
    }

    private sealed class PeriodMemberRow
    {
        public long MemberId { get; set; }
        public long SiteId { get; set; }
        public long Groupnum { get; set; }
        public long CardNumber { get; set; }
        public string Name { get; set; } = "";
        public string CarNumber { get; set; } = "";
        public int CarType { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
