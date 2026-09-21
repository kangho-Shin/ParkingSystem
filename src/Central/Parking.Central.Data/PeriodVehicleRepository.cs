using Dapper;
using MySqlConnector;
using Newtonsoft.Json;
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
        PeriodMemberRow? row = await connection.QueryFirstOrDefaultAsync<PeriodMemberRow>(new CommandDefinition("""
            SELECT xindex MemberId, sitenum SiteId, @Groupnum Groupnum,
                   cardid CardId, COALESCE(name,'') Name,
                   CASE WHEN carnum1=@CarNumber THEN carnum1 ELSE carnum2 END CarNumber,
                   CASE WHEN carnum1=@CarNumber THEN cartype1 ELSE cartype2 END CarType,
                   enddate EndDate
            FROM tperiodmember
            WHERE sitenum=@SiteId AND useflag<>0
              AND (carnum1=@CarNumber OR carnum2=@CarNumber)
              AND (startdate IS NULL OR startdate<=@CheckDate)
              AND (enddate IS NULL OR enddate>=@CheckDate)
              AND (groupnum=@Groupnum OR SUBSTRING(COALESCE(parkarea,''),@Groupnum,1)='1')
            ORDER BY enddate DESC, xindex DESC
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
        return row is null ? null : new PeriodMember(
            row.MemberId,
            row.SiteId,
            checked((int)row.Groupnum),
            row.CardId,
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
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        byte[] eventId = request.EventId.ToByteArray();

        try
        {
            int inserted = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT IGNORE INTO parking_event
                (eventid,sitenum,groupnum,laneid,deviceid,eventtype,carnum,eventat,imagepath)
                VALUES
                (@EventId,@SiteId,@Groupnum,@LaneId,@DeviceId,@EventType,
                 @CarNumber,@InDateTimeUtc,@InImage);
                """, new
            {
                EventId = eventId,
                request.SiteId,
                request.Groupnum,
                request.LaneId,
                request.DeviceId,
                request.EventType,
                request.CarNumber,
                InDateTimeUtc = request.InDateTime.UtcDateTime,
                InImage = VehicleImageName.FileNameOnly(request.InImage)
            }, transaction, cancellationToken: cancellationToken));

            if (inserted == 0)
            {
                string resultJson = await connection.QuerySingleAsync<string>(new CommandDefinition(
                    "SELECT resultjson FROM parking_event WHERE eventid=@EventId;",
                    new { EventId = eventId }, transaction, cancellationToken: cancellationToken));
                await transaction.CommitAsync(cancellationToken);
                return JsonConvert.DeserializeObject<FieldEventResponse>(resultJson)
                    ?? throw new InvalidOperationException("기존 등록차량 입차결과를 읽지 못했습니다.");
            }

            long? existingId = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition("""
                SELECT xindex FROM tperiodinout
                WHERE sitenum=@SiteId AND groupnum=@Groupnum
                  AND carnum=@CarNumber AND outflag<>'O'
                ORDER BY indatetime DESC LIMIT 1 FOR UPDATE;
                """, new { request.SiteId, request.Groupnum, request.CarNumber },
                transaction, cancellationToken: cancellationToken));

            long periodSessionId = existingId ?? await connection.ExecuteScalarAsync<long>(
                new CommandDefinition("""
                    INSERT INTO tperiodinout
                    (sitenum,groupnum,memberindex,cardid,name,carnum,cartype,
                     ineventid,inlaneid,indeviceid,indatetime,inimage,enddate,outflag)
                    VALUES
                    (@SiteId,@Groupnum,@MemberId,@CardId,@Name,@CarNumber,@CarType,
                     @EventId,@LaneId,@DeviceId,@InDateTimeUtc,@InImage,@EndDate,'I');
                    SELECT LAST_INSERT_ID();
                    """, new
                {
                    request.SiteId,
                    request.Groupnum,
                    member.MemberId,
                    member.CardId,
                    member.Name,
                    request.CarNumber,
                    member.CarType,
                    EventId = eventId,
                    request.LaneId,
                    request.DeviceId,
                    InDateTimeUtc = request.InDateTime.UtcDateTime,
                    InImage = VehicleImageName.FileNameOnly(request.InImage),
                    member.EndDate
                }, transaction, cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE tperiodmember SET outflag='I' WHERE xindex=@MemberId;",
                new { member.MemberId }, transaction, cancellationToken: cancellationToken));

            FieldEventResponse response = new(
                request.EventId, true, periodSessionId,
                existingId.HasValue ? "PERIOD_ENTRY_DUPLICATE" : "PERIOD_ENTRY_ACCEPTED",
                "등록차량 입차가 처리되었습니다.", true);
            await SaveResultAsync(connection, transaction, eventId, response, cancellationToken);
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
                SELECT xindex PeriodSessionId, memberindex MemberId, sitenum SiteId,
                       groupnum Groupnum, carnum CarNumber, indatetime InDateTime,
                       inimage InImage, outflag OutFlag
                FROM tperiodinout
                WHERE sitenum=@SiteId AND groupnum=@Groupnum
                  AND carnum=@CarNumber AND outflag<>'O'
                ORDER BY indatetime DESC LIMIT 1;
                """, new { SiteId = siteId, Groupnum = groupnum, CarNumber = carNumber.Trim() },
                cancellationToken: cancellationToken));
        return row is null ? null : new OpenPeriodSession(
            row.PeriodSessionId,
            row.MemberId,
            row.SiteId,
            row.Groupnum,
            row.CarNumber,
            new DateTimeOffset(DateTime.SpecifyKind(row.InDateTime, DateTimeKind.Utc)),
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
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        byte[] eventId = request.EventId.ToByteArray();

        try
        {
            int inserted = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT IGNORE INTO parking_event
                (eventid,sitenum,groupnum,laneid,deviceid,eventtype,carnum,eventat,imagepath)
                VALUES
                (@EventId,@SiteId,@Groupnum,@LaneId,@DeviceId,@EventType,
                 @CarNumber,@OutDateTimeUtc,@OutImage);
                """, new
            {
                EventId = eventId,
                request.SiteId,
                request.Groupnum,
                request.LaneId,
                request.DeviceId,
                request.EventType,
                request.CarNumber,
                OutDateTimeUtc = request.OutDateTime.UtcDateTime,
                OutImage = VehicleImageName.FileNameOnly(request.OutImage)
            }, transaction, cancellationToken: cancellationToken));

            if (inserted == 0)
            {
                string resultJson = await connection.QuerySingleAsync<string>(new CommandDefinition(
                    "SELECT resultjson FROM parking_event WHERE eventid=@EventId;",
                    new { EventId = eventId }, transaction, cancellationToken: cancellationToken));
                await transaction.CommitAsync(cancellationToken);
                return JsonConvert.DeserializeObject<FieldEventResponse>(resultJson)
                    ?? throw new InvalidOperationException("기존 등록차량 출차결과를 읽지 못했습니다.");
            }

            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE tperiodinout
                SET outeventid=@EventId, outlaneid=@LaneId, outdeviceid=@DeviceId,
                    outdatetime=@OutDateTimeUtc, outimage=@OutImage,
                    parktime=GREATEST(TIMESTAMPDIFF(MINUTE,indatetime,@OutDateTimeUtc),0),
                    outflag='O'
                WHERE xindex=@PeriodSessionId AND outflag<>'O';
                """, new
            {
                EventId = eventId,
                request.LaneId,
                request.DeviceId,
                OutDateTimeUtc = request.OutDateTime.UtcDateTime,
                OutImage = VehicleImageName.FileNameOnly(request.OutImage),
                session.PeriodSessionId
            }, transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE tperiodmember SET outflag='O' WHERE xindex=@MemberId;",
                new { session.MemberId }, transaction, cancellationToken: cancellationToken));

            FieldEventResponse response = new(
                request.EventId, true, session.PeriodSessionId,
                "PERIOD_EXIT_ACCEPTED", "등록차량 출차가 처리되었습니다.", true);
            await SaveResultAsync(connection, transaction, eventId, response, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
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
            UPDATE parking_event SET resultjson=@ResultJson WHERE eventid=@EventId;
            """, new
        {
            EventId = eventId,
            ResultJson = JsonConvert.SerializeObject(response)
        }, transaction, cancellationToken: cancellationToken));

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
        public long CardId { get; set; }
        public string Name { get; set; } = "";
        public string CarNumber { get; set; } = "";
        public string CarType { get; set; } = "";
        public DateTime? EndDate { get; set; }
    }
}
