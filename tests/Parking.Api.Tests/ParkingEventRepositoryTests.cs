using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public class ParkingEventRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 같은_EventId는_주차내역을_중복생성하지_않는다()
    {
        await ClearTablesAsync();
        FieldEventRequest request = new(
            Guid.NewGuid(), 9001, 9010, 4001, "12가3456", DateTimeOffset.UtcNow, 2);
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse first = await repository.SaveEntryAsync(request, CancellationToken.None);
        FieldEventResponse second = await repository.SaveEntryAsync(request, CancellationToken.None);

        Assert.Equal(first.ParkingSessionId, second.ParkingSessionId);
        Assert.Equal(1, await GetSessionCountAsync());
        Assert.Equal(1, await GetGeneralEntryCountAsync());
        Assert.Equal(0, await GetGeneralExitCountAsync());
    }

    [Fact]
    public async Task 입차시_OutFlag는_I이다()
    {
        await ClearTablesAsync();
        ParkingEventRepository repository = new(ConnectionString);
        FieldEventResponse result = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(),
                9001,
                9010,
                4001,
                "34나5678",
                DateTimeOffset.UtcNow,
                2),
            CancellationToken.None);

        Assert.Equal("I", await GetOutFlagAsync(result.ParkingSessionId!.Value));
    }

    [Fact]
    public async Task 입차시_그룹_장비_InImage를_저장한다()
    {
        await ClearTablesAsync();
        const string inImage = @"C:\ParkingSystem\Images\9001_002_401_9010_Entry_test.jpg";
        FieldEventRequest request = new(
            Guid.NewGuid(), 9001, 9010, 4001, "56다7890", DateTimeOffset.UtcNow,
            2, ParkingEventType.Entry, inImage);
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveEntryAsync(
            request,
            CancellationToken.None);

        await using MySqlConnection connection = new(ConnectionString);
        EntryStorageRow stored = await connection.QuerySingleAsync<EntryStorageRow>("""
            SELECT s.groupnum Groupnum,s.indevicenum InDeviceNumber,
                   e.eventid EventId,
                   s.inimage InImage, e.image EventImage
            FROM tparkinfo s
            JOIN tparkevent e ON e.eventid=s.ineventid
            WHERE s.xindex=@ParkingSessionId;
            """, new { ParkingSessionId = result.ParkingSessionId });

        Assert.Equal(2, stored.Groupnum);
        Assert.Equal(401, stored.InDeviceNumber);
        Assert.Equal(request.EventId.ToString("N"), stored.EventId);
        Assert.Equal("9001_002_401_9010_Entry_test.jpg", stored.InImage);
        Assert.Equal("9001_002_401_9010_Entry_test.jpg", stored.EventImage);
    }

    [Fact]
    public async Task 같은차량_10초이내_재인식은_기존세션을_반환한다()
    {
        await ClearTablesAsync();
        await SetDuplicateEntryTimeAsync(10);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        (long oldSessionId, _) = await CreateExistingSessionAsync(
            "77가7777", "I", now.AddSeconds(-5));
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 9001, 9010, 4001, "77가7777", now,
                2, ParkingEventType.Entry, "NEW.jpg"),
            CancellationToken.None);

        Assert.Equal(oldSessionId, result.ParkingSessionId);
        Assert.Equal("ENTRY_DUPLICATE", result.ResultCode);
        Assert.Equal(1, await GetSessionCountAsync());
        Assert.Equal(1, await GetGeneralEntryCountAsync());
        Assert.Equal(0, await GetGeneralExitCountAsync());
    }

    [Fact]
    public async Task 오래된_I차량은_이전세션을_삭제후_새로_입차하고_원본이벤트는_보존한다()
    {
        await ClearTablesAsync();
        await SetDuplicateEntryTimeAsync(10);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        (long oldSessionId, Guid oldEventId) = await CreateExistingSessionAsync(
            "88나8888", "I", now.AddSeconds(-20));
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 9001, 9010, 4001, "88나8888", now,
                2, ParkingEventType.Entry, "NEW.jpg"),
            CancellationToken.None);

        Assert.NotEqual(oldSessionId, result.ParkingSessionId);
        await using MySqlConnection connection = new(ConnectionString);
        Assert.Equal(0, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM tparkinfo WHERE xindex=@OldSessionId;",
            new { OldSessionId = oldSessionId }));
        Assert.Equal(1, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM tparkevent WHERE eventid=@OldEventId;",
            new { OldEventId = oldEventId.ToString("N") }));
        Assert.Equal(2, await GetGeneralEntryCountAsync());
        Assert.Equal(1, await GetGeneralExitCountAsync());
    }

    [Fact]
    public async Task X차량은_O로_마감후_새로운_I입차를_생성한다()
    {
        await ClearTablesAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        (long oldSessionId, _) = await CreateExistingSessionAsync(
            "99다9999", "X", now.AddMinutes(-10));
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 9001, 9010, 4001, "99다9999", now,
                2, ParkingEventType.Entry, "NEW.jpg"),
            CancellationToken.None);

        await using MySqlConnection connection = new(ConnectionString);
        Assert.Equal("O", await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM tparkinfo WHERE xindex=@OldSessionId;",
            new { OldSessionId = oldSessionId }));
        Assert.Equal("I", await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM tparkinfo WHERE xindex=@NewSessionId;",
            new { NewSessionId = result.ParkingSessionId }));
        Assert.Equal(2, await GetGeneralEntryCountAsync());
        Assert.Equal(1, await GetGeneralExitCountAsync());
    }

    private static async Task ClearTablesAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync(
            """
            DELETE FROM tdiscountinfo;
            DELETE FROM tbcardinfo;
            DELETE FROM tparkinfo;
            DELETE FROM tparkevent;
            UPDATE tparkingnum
            SET inilbancnt=0,outilbancnt=0,inregcnt=0,outregcnt=0
            WHERE sitenum=9001 AND groupnum=2;
            """);
    }

    private static async Task<long> GetSessionCountAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM tparkinfo;");
    }

    private static async Task<long> GetGeneralEntryCountAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>(
            "SELECT inilbancnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;");
    }

    private static async Task<long> GetGeneralExitCountAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>(
            "SELECT outilbancnt FROM tparkingnum WHERE sitenum=9001 AND groupnum=2;");
    }

    private static async Task SetDuplicateEntryTimeAsync(int seconds)
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tparkvariable(sitenum,groupnum,cmdtype,val,opt,msg)
            VALUES (9001,2,'CMD_DUPLICATE_ENTRY_TIME','0',@Seconds,NULL)
            ON DUPLICATE KEY UPDATE opt=VALUES(opt);
            """, new { Seconds = seconds.ToString() });
    }

    private static async Task<(long ParkingSessionId, Guid EventId)> CreateExistingSessionAsync(
        string carNumber,
        string outFlag,
        DateTimeOffset inDateTime)
    {
        Guid eventId = Guid.NewGuid();
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tparkevent
            (eventid,sitenum,groupnum,laneid,deviceid,devicenum,eventtype,carnum,eventdate,status)
            VALUES (@EventId,9001,2,9010,4001,401,'ENTRY',@CarNumber,@InDateTime,'COMPLETE');
            """, new
        {
            EventId = eventId.ToString("N"),
            CarNumber = carNumber,
            InDateTime = ParkingLocalTime.ToDatabase(inDateTime)
        });
        long parkingSessionId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO tparkinfo
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,indate,outflag)
            VALUES (9001,@EventId,@CarNumber,2,1,9010,401,@InDateTime,@OutFlag);
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = eventId.ToString("N"),
            CarNumber = carNumber,
            InDateTime = ParkingLocalTime.ToDatabase(inDateTime),
            OutFlag = outFlag
        });
        await connection.ExecuteAsync("""
            UPDATE tparkingnum
            SET inilbancnt=inilbancnt+1
            WHERE sitenum=9001 AND groupnum=2;
            """);
        return (parkingSessionId, eventId);
    }

    private static async Task<string> GetOutFlagAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM tparkinfo WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }

    private sealed class EntryStorageRow
    {
        public int Groupnum { get; set; }
        public int InDeviceNumber { get; set; }
        public string EventId { get; set; } = "";
        public string? InImage { get; set; }
        public string? EventImage { get; set; }
    }
}
