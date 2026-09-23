using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class PeriodVehicleRepositoryTests : IAsyncLifetime
{
    private const long SiteId = 990104;
    private const int Groupnum = 2;
    private const long EntryLaneId = 990141;
    private const long ExitLaneId = 990142;
    private const long EntryDeviceId = 9901401;
    private const long ExitDeviceId = 9901402;

    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 등록차량은_tperiodinout에_입차후_같은행에_출차한다()
    {
        await using (MySqlConnection connection = new(ConnectionString))
        {
            await connection.ExecuteAsync("""
                INSERT INTO tperiodmember
                (sitenum,groupnum,cardno,name,carnum1,cartype1,startdate,enddate,
                 parkarea,useflag,outflag)
                VALUES
                (@SiteId,@Groupnum,100,'등록회원','12가3456',1,CURDATE()-INTERVAL 1 DAY,
                 CURDATE()+INTERVAL 30 DAY,'0100000',1,'O');
                """, new { SiteId, Groupnum });
        }
        PeriodVehicleRepository repository = new(ConnectionString);
        DateTimeOffset inDateTime = DateTimeOffset.UtcNow;
        PeriodMember? member = await repository.FindMemberAsync(
            SiteId, Groupnum, "12가3456", inDateTime, CancellationToken.None);

        Assert.NotNull(member);
        FieldEventResponse entry = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), SiteId, EntryLaneId, EntryDeviceId, "12가3456", inDateTime,
                Groupnum, ParkingEventType.Entry, @"C:\Images\PERIOD-IN.jpg", 3, true),
            member,
            CancellationToken.None);
        OpenPeriodSession? open = await repository.FindOpenAsync(
            SiteId, Groupnum, "12가3456", CancellationToken.None);

        Assert.NotNull(open);
        Assert.Equal(entry.ParkingSessionId, open.PeriodSessionId);
        Assert.Equal("PERIOD-IN.jpg", open.InImage);

        FieldEventResponse exit = await repository.SaveExitAsync(
            new ExitEventRequest(
                Guid.NewGuid(), SiteId, ExitLaneId, ExitDeviceId, "12가3456", inDateTime.AddMinutes(30),
                Groupnum, 1, null, ParkingEventType.Exit, @"C:\Images\PERIOD-OUT.jpg"),
            open,
            CancellationToken.None);

        Assert.Equal("PERIOD_EXIT_ACCEPTED", exit.ResultCode);
        await using MySqlConnection verifyConnection = new(ConnectionString);
        PeriodExitRow stored = await verifyConnection.QuerySingleAsync<PeriodExitRow>("""
            SELECT outflag OutFlag, inimage InImage, outimage OutImage,
                   parktime ParkTime,cartype CarType,manual Manual
            FROM tperiodinout WHERE xindex=@PeriodSessionId;
            """, new { open.PeriodSessionId });
        Assert.Equal("O", stored.OutFlag);
        Assert.Equal("PERIOD-IN.jpg", stored.InImage);
        Assert.Equal("PERIOD-OUT.jpg", stored.OutImage);
        Assert.Equal(30, stored.ParkTime);
        Assert.Equal(1, stored.CarType);
        Assert.Equal(1, stored.Manual);
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<long>(
            "SELECT inregcnt FROM tparkingnum WHERE sitenum=@SiteId AND groupnum=@Groupnum;",
            new { SiteId, Groupnum }));
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<long>(
            "SELECT outregcnt FROM tparkingnum WHERE sitenum=@SiteId AND groupnum=@Groupnum;",
            new { SiteId, Groupnum }));

        FieldEventResponse repeatedExit = await repository.SaveExitAsync(
            new ExitEventRequest(
                Guid.NewGuid(), SiteId, ExitLaneId, ExitDeviceId, "12가3456", inDateTime.AddMinutes(31),
                Groupnum),
            open,
            CancellationToken.None);
        Assert.False(repeatedExit.Accepted);
        Assert.Equal("PERIOD_SESSION_NOT_OPEN", repeatedExit.ResultCode);
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<long>(
            "SELECT outregcnt FROM tparkingnum WHERE sitenum=@SiteId AND groupnum=@Groupnum;",
            new { SiteId, Groupnum }));
    }

    [Fact]
    public async Task 등록차량_중복입차는_기존행을_자동출차하고_새행을_생성한다()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tperiodmember
            (sitenum,groupnum,cardno,name,carnum1,cartype1,startdate,enddate,
             parkarea,useflag,outflag)
            VALUES
            (@SiteId,@Groupnum,200,'중복회원','34나5678',1,CURDATE()-INTERVAL 1 DAY,
             CURDATE()+INTERVAL 30 DAY,'0100000',1,'O');
            """, new { SiteId, Groupnum });
        PeriodVehicleRepository repository = new(ConnectionString);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PeriodMember member = await repository.FindMemberAsync(
            SiteId, Groupnum, "34나5678", now, CancellationToken.None)
            ?? throw new InvalidOperationException("등록차량이 없습니다.");

        FieldEventResponse first = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), SiteId, EntryLaneId, EntryDeviceId, "34나5678",
                now.AddMinutes(-30), Groupnum),
            member,
            CancellationToken.None);
        FieldEventResponse second = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), SiteId, EntryLaneId, EntryDeviceId, "34나5678", now, Groupnum),
            member,
            CancellationToken.None);

        IReadOnlyList<DuplicatePeriodRow> rows = (await connection.QueryAsync<DuplicatePeriodRow>("""
            SELECT xindex PeriodSessionId,outflag OutFlag,note Note
            FROM tperiodinout
            WHERE periodindex=@MemberId
            ORDER BY xindex;
            """, new { member.MemberId })).AsList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(first.ParkingSessionId, rows[0].PeriodSessionId);
        Assert.Equal("O", rows[0].OutFlag);
        Assert.Equal("DUPLICATE_ENTRY", rows[0].Note);
        Assert.Equal(second.ParkingSessionId, rows[1].PeriodSessionId);
        Assert.Equal("I", rows[1].OutFlag);
        Assert.Equal(2, await connection.ExecuteScalarAsync<long>(
            "SELECT inregcnt FROM tparkingnum WHERE sitenum=@SiteId AND groupnum=@Groupnum;",
            new { SiteId, Groupnum }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<long>(
            "SELECT outregcnt FROM tparkingnum WHERE sitenum=@SiteId AND groupnum=@Groupnum;",
            new { SiteId, Groupnum }));
    }

    public async Task InitializeAsync()
    {
        await ClearAsync();
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tparkings(sitenum,groupnum,parkname,parktype,sitekeyhash,useflag)
            VALUES(@SiteId,@Groupnum,'등록차량 저장소 시험','TEST',REPEAT('0',64),1);
            INSERT INTO tlaneinfo(laneid,sitenum,groupnum,lanename,direction,useflag) VALUES
            (@EntryLaneId,@SiteId,@Groupnum,'시험 입차','ENTRY',1),
            (@ExitLaneId,@SiteId,@Groupnum,'시험 출차','EXIT',1);
            INSERT INTO tdeviceinfo
            (deviceid,sitenum,groupnum,laneid,devicenum,devicename,devicetype,useflag) VALUES
            (@EntryDeviceId,@SiteId,@Groupnum,@EntryLaneId,401,'시험 입차LPR',3,1),
            (@ExitDeviceId,@SiteId,@Groupnum,@ExitLaneId,402,'시험 출차LPR',3,1);
            INSERT INTO tparkingnum
            (sitenum,groupnum,inilbancnt,outilbancnt,inregcnt,outregcnt,ilbanfullnum,regfullnum)
            VALUES(@SiteId,@Groupnum,0,0,0,0,0,0);
            """, new
        {
            SiteId,
            Groupnum,
            EntryLaneId,
            ExitLaneId,
            EntryDeviceId,
            ExitDeviceId
        });
    }

    public Task DisposeAsync() => ClearAsync();

    private static async Task ClearAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            DELETE FROM tparkevent WHERE sitenum=@SiteId;
            DELETE FROM tperiodinout WHERE sitenum=@SiteId;
            DELETE FROM tperiodmember WHERE sitenum=@SiteId;
            DELETE FROM tdeviceinfo WHERE sitenum=@SiteId;
            DELETE FROM tlaneinfo WHERE sitenum=@SiteId;
            DELETE FROM tparkings WHERE sitenum=@SiteId;
            DELETE FROM tparkingnum WHERE sitenum=@SiteId;
            """, new { SiteId });
    }

    private sealed class PeriodExitRow
    {
        public string OutFlag { get; set; } = "";
        public string? InImage { get; set; }
        public string? OutImage { get; set; }
        public int ParkTime { get; set; }
        public int CarType { get; set; }
        public int Manual { get; set; }
    }

    private sealed class DuplicatePeriodRow
    {
        public long PeriodSessionId { get; set; }
        public string OutFlag { get; set; } = "";
        public string? Note { get; set; }
    }
}
