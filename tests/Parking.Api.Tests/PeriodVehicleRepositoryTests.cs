using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class PeriodVehicleRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 등록차량은_tperiodinout에_입차후_같은행에_출차한다()
    {
        await ClearAsync();
        await using (MySqlConnection connection = new(ConnectionString))
        {
            await connection.ExecuteAsync("""
                INSERT INTO tperiodmember
                (sitenum,groupnum,cardno,name,carnum1,cartype1,startdate,enddate,
                 parkarea,useflag,outflag)
                VALUES
                (9001,2,100,'등록회원','12가3456',1,CURDATE()-INTERVAL 1 DAY,
                 CURDATE()+INTERVAL 30 DAY,'0100000',1,'O');
                """);
        }
        PeriodVehicleRepository repository = new(ConnectionString);
        DateTimeOffset inDateTime = DateTimeOffset.UtcNow;
        PeriodMember? member = await repository.FindMemberAsync(
            9001, 2, "12가3456", inDateTime, CancellationToken.None);

        Assert.NotNull(member);
        FieldEventResponse entry = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 9001, 9010, 4001, "12가3456", inDateTime,
                2, ParkingEventType.Entry, @"C:\Images\PERIOD-IN.jpg", 3, true),
            member,
            CancellationToken.None);
        OpenPeriodSession? open = await repository.FindOpenAsync(
            9001, 2, "12가3456", CancellationToken.None);

        Assert.NotNull(open);
        Assert.Equal(entry.ParkingSessionId, open.PeriodSessionId);
        Assert.Equal("PERIOD-IN.jpg", open.InImage);

        FieldEventResponse exit = await repository.SaveExitAsync(
            new ExitEventRequest(
                Guid.NewGuid(), 9001, 9020, 4002, "12가3456", inDateTime.AddMinutes(30),
                2, 1, null, ParkingEventType.Exit, @"C:\Images\PERIOD-OUT.jpg"),
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
            "SELECT inregcnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;"));
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<long>(
            "SELECT outregcnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;"));

        FieldEventResponse repeatedExit = await repository.SaveExitAsync(
            new ExitEventRequest(
                Guid.NewGuid(), 9001, 9020, 4002, "12가3456", inDateTime.AddMinutes(31),
                2),
            open,
            CancellationToken.None);
        Assert.False(repeatedExit.Accepted);
        Assert.Equal("PERIOD_SESSION_NOT_OPEN", repeatedExit.ResultCode);
        Assert.Equal(1, await verifyConnection.ExecuteScalarAsync<long>(
            "SELECT outregcnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;"));
    }

    [Fact]
    public async Task 등록차량_중복입차는_기존행을_자동출차하고_새행을_생성한다()
    {
        await ClearAsync();
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tperiodmember
            (sitenum,groupnum,cardno,name,carnum1,cartype1,startdate,enddate,
             parkarea,useflag,outflag)
            VALUES
            (9001,2,200,'중복회원','34나5678',1,CURDATE()-INTERVAL 1 DAY,
             CURDATE()+INTERVAL 30 DAY,'0100000',1,'O');
            """);
        PeriodVehicleRepository repository = new(ConnectionString);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PeriodMember member = await repository.FindMemberAsync(
            9001, 2, "34나5678", now, CancellationToken.None)
            ?? throw new InvalidOperationException("등록차량이 없습니다.");

        FieldEventResponse first = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 9001, 9010, 4001, "34나5678",
                now.AddMinutes(-30), 2),
            member,
            CancellationToken.None);
        FieldEventResponse second = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 9001, 9010, 4001, "34나5678", now, 2),
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
            "SELECT inregcnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;"));
        Assert.Equal(1, await connection.ExecuteScalarAsync<long>(
            "SELECT outregcnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;"));
    }

    private static async Task ClearAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            DELETE FROM tperiodinout;
            DELETE FROM tperiodmember;
            DELETE FROM tparkevent;
            UPDATE tparkingnum
            SET inilbancnt=0,outilbancnt=0,inregcnt=0,outregcnt=0
            WHERE sitenum=9001 AND groupnum=2;
            """);
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
