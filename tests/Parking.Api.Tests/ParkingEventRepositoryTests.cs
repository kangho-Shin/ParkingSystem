using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
public class ParkingEventRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 같은_EventId는_주차내역을_중복생성하지_않는다()
    {
        await ClearTablesAsync();
        FieldEventRequest request = new(
            Guid.NewGuid(), 1, 10, 101, "12가3456", DateTimeOffset.UtcNow);
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse first = await repository.SaveEntryAsync(request, CancellationToken.None);
        FieldEventResponse second = await repository.SaveEntryAsync(request, CancellationToken.None);

        Assert.Equal(first.ParkingSessionId, second.ParkingSessionId);
        Assert.Equal(1, await GetSessionCountAsync());
    }

    [Fact]
    public async Task 입차시_OutFlag는_I이다()
    {
        await ClearTablesAsync();
        ParkingEventRepository repository = new(ConnectionString);
        FieldEventResponse result = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(),
                1,
                10,
                101,
                "34나5678",
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal("I", await GetOutFlagAsync(result.ParkingSessionId!.Value));
    }

    [Fact]
    public async Task 입차시_그룹_장비_InImage를_저장한다()
    {
        await ClearTablesAsync();
        const string inImage = @"C:\ParkingSystem\Images\001_002_010_Entry_test.jpg";
        FieldEventRequest request = new(
            Guid.NewGuid(), 1, 10, 101, "56다7890", DateTimeOffset.UtcNow,
            2, ParkingEventType.Entry, inImage);
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveEntryAsync(
            request,
            CancellationToken.None);

        await using MySqlConnection connection = new(ConnectionString);
        EntryStorageRow stored = await connection.QuerySingleAsync<EntryStorageRow>("""
            SELECT s.groupnum Groupnum, s.indeviceid InDeviceId,
                   s.inimage InImage, e.imagepath EventImage
            FROM parking_session s
            JOIN parking_event e ON e.eventid=s.ineventid
            WHERE s.xindex=@ParkingSessionId;
            """, new { ParkingSessionId = result.ParkingSessionId });

        Assert.Equal(2, stored.Groupnum);
        Assert.Equal(101, stored.InDeviceId);
        Assert.Equal("001_002_010_Entry_test.jpg", stored.InImage);
        Assert.Equal("001_002_010_Entry_test.jpg", stored.EventImage);
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
                Guid.NewGuid(), 1, 10, 101, "77가7777", now,
                1, ParkingEventType.Entry, "NEW.jpg"),
            CancellationToken.None);

        Assert.Equal(oldSessionId, result.ParkingSessionId);
        Assert.Equal("ENTRY_DUPLICATE", result.ResultCode);
        Assert.Equal(1, await GetSessionCountAsync());
    }

    [Fact]
    public async Task 오래된_I차량은_이전세션과_입차이벤트를_삭제후_새로_입차한다()
    {
        await ClearTablesAsync();
        await SetDuplicateEntryTimeAsync(10);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        (long oldSessionId, Guid oldEventId) = await CreateExistingSessionAsync(
            "88나8888", "I", now.AddSeconds(-20));
        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 1, 10, 101, "88나8888", now,
                1, ParkingEventType.Entry, "NEW.jpg"),
            CancellationToken.None);

        Assert.NotEqual(oldSessionId, result.ParkingSessionId);
        await using MySqlConnection connection = new(ConnectionString);
        Assert.Equal(0, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM parking_session WHERE xindex=@OldSessionId;",
            new { OldSessionId = oldSessionId }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM parking_event WHERE eventid=@OldEventId;",
            new { OldEventId = oldEventId.ToByteArray() }));
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
                Guid.NewGuid(), 1, 10, 101, "99다9999", now,
                1, ParkingEventType.Entry, "NEW.jpg"),
            CancellationToken.None);

        await using MySqlConnection connection = new(ConnectionString);
        Assert.Equal("O", await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@OldSessionId;",
            new { OldSessionId = oldSessionId }));
        Assert.Equal("I", await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@NewSessionId;",
            new { NewSessionId = result.ParkingSessionId }));
    }

    private static async Task ClearTablesAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync(
            """
            DELETE FROM parking_session_discount;
            DELETE FROM payment;
            DELETE FROM parking_session;
            DELETE FROM parking_event;
            """);
    }

    private static async Task<long> GetSessionCountAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM parking_session;");
    }

    private static async Task SetDuplicateEntryTimeAsync(int seconds)
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            INSERT INTO tparkvariable(sitenum,groupnum,cmd_type,val,opt,msg)
            VALUES (1,1,'CMD_DUPLICATE_ENTRY_TIME','0',@Seconds,NULL)
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
            INSERT INTO parking_event
            (eventid,sitenum,groupnum,laneid,deviceid,eventtype,carnum,eventat)
            VALUES (@EventId,1,1,10,101,'Entry',@CarNumber,@InDateTime);
            """, new
        {
            EventId = eventId.ToByteArray(),
            CarNumber = carNumber,
            InDateTime = inDateTime.UtcDateTime
        });
        long parkingSessionId = await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indeviceid,indate,outflag)
            VALUES (1,@EventId,@CarNumber,1,1,10,101,@InDateTime,@OutFlag);
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = eventId.ToByteArray(),
            CarNumber = carNumber,
            InDateTime = inDateTime.UtcDateTime,
            OutFlag = outFlag
        });
        return (parkingSessionId, eventId);
    }

    private static async Task<string> GetOutFlagAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }

    private sealed class EntryStorageRow
    {
        public int Groupnum { get; set; }
        public long InDeviceId { get; set; }
        public string? InImage { get; set; }
        public string? EventImage { get; set; }
    }
}
