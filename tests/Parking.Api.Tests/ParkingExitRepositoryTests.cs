using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
public sealed class ParkingExitRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 미출차_조회는_입차시_저장한_그룹과_차종을_반환한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "I", 2, 3);
        ParkingExitRepository repository = new(ConnectionString);

        OpenParkingSessionResponse? result = await repository.FindOpenAsync(
            1,
            carNumber,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(parkingSessionId, result.ParkingSessionId);
        Assert.Equal(2, result.Groupnum);
        Assert.Equal(3, result.CarType);
    }

    [Fact]
    public async Task 미결제_차량은_출차를_거부한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "I");
        ExitEventRequest request = CreateRequest(carNumber);
        ParkingExitRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveExitAsync(
            request,
            false,
            CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.False(result.OpenBarrier);
        Assert.Equal("PAYMENT_REQUIRED", result.ResultCode);
        Assert.Equal("I", await GetSessionOutFlagAsync(parkingSessionId));
    }

    [Fact]
    public async Task 계산요금이_0원이면_미결제_차량도_출차를_허용한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "I");
        ParkingExitRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveExitAsync(
            CreateRequest(carNumber),
            true,
            CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.True(result.OpenBarrier);
        Assert.Equal("EXIT_ACCEPTED", result.ResultCode);
        Assert.Equal("O", await GetSessionOutFlagAsync(parkingSessionId));
    }

    [Fact]
    public async Task 결제완료_차량은_출차를_허용한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "X");
        ParkingExitRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveExitAsync(
            CreateRequest(carNumber),
            true,
            CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.True(result.OpenBarrier);
        Assert.Equal("O", await GetSessionOutFlagAsync(parkingSessionId));
    }

    [Fact]
    public async Task 결제상태라도_추가요금이_있으면_출차를_거부한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "X");
        ParkingExitRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveExitAsync(
            CreateRequest(carNumber),
            false,
            CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.False(result.OpenBarrier);
        Assert.Equal("PAYMENT_REQUIRED", result.ResultCode);
        Assert.Equal("X", await GetSessionOutFlagAsync(parkingSessionId));
    }

    [Fact]
    public async Task 출차시_그룹_장비_OutImage를_저장한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "X", 2);
        const string outImage = @"C:\ParkingSystem\Images\001_002_020_Exit_test.jpg";
        ExitEventRequest request = new(
            Guid.NewGuid(), 1, 20, 201, carNumber, DateTimeOffset.UtcNow,
            2, 1, null, ParkingEventType.Exit, outImage);
        ParkingExitRepository repository = new(ConnectionString);

        await repository.SaveExitAsync(request, true, CancellationToken.None);

        await using MySqlConnection connection = new(ConnectionString);
        ExitStorageRow stored = await connection.QuerySingleAsync<ExitStorageRow>("""
            SELECT s.outdeviceid OutDeviceId, s.outimage OutImage,
                   e.groupnum Groupnum, e.imagepath EventImage
            FROM parking_session s
            JOIN parking_event e ON e.eventid=s.outeventid
            WHERE s.xindex=@ParkingSessionId;
            """, new { ParkingSessionId = parkingSessionId });

        Assert.Equal(2, stored.Groupnum);
        Assert.Equal(201, stored.OutDeviceId);
        Assert.Equal("001_002_020_Exit_test.jpg", stored.OutImage);
        Assert.Equal("001_002_020_Exit_test.jpg", stored.EventImage);
    }

    private static ExitEventRequest CreateRequest(string carNumber) => new(
        Guid.NewGuid(),
        1,
        20,
        201,
        carNumber,
        DateTimeOffset.UtcNow);

    private sealed class ExitStorageRow
    {
        public int Groupnum { get; set; }
        public long OutDeviceId { get; set; }
        public string? OutImage { get; set; }
        public string? EventImage { get; set; }
    }

    private static async Task<long> CreateSessionAsync(
        string carNumber,
        string outFlag,
        int groupnum = 1,
        int carType = 1)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indate,outflag)
            VALUES (1,@EntryEventId,@CarNumber,@Groupnum,@CarType,10,UTC_TIMESTAMP(6),@OutFlag);
            SELECT LAST_INSERT_ID();
            """, new
            {
                EntryEventId = Guid.NewGuid().ToByteArray(),
                CarNumber = carNumber,
                Groupnum = groupnum,
                CarType = carType,
                OutFlag = outFlag
            });
    }

    private static async Task<string> GetSessionOutFlagAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }
}
