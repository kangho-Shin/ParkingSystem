using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
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
                (sitenum,groupnum,cardid,name,carnum1,cartype1,startdate,enddate,
                 parkarea,useflag,outflag)
                VALUES
                (1,1,100,'등록회원','12가3456','승용',CURDATE()-INTERVAL 1 DAY,
                 CURDATE()+INTERVAL 30 DAY,'11000000',1,'O');
                """);
        }
        PeriodVehicleRepository repository = new(ConnectionString);
        DateTimeOffset inDateTime = DateTimeOffset.UtcNow;
        PeriodMember? member = await repository.FindMemberAsync(
            1, 1, "12가3456", inDateTime, CancellationToken.None);

        Assert.NotNull(member);
        FieldEventResponse entry = await repository.SaveEntryAsync(
            new FieldEventRequest(
                Guid.NewGuid(), 1, 10, 101, "12가3456", inDateTime,
                1, ParkingEventType.Entry, @"C:\Images\PERIOD-IN.jpg"),
            member,
            CancellationToken.None);
        OpenPeriodSession? open = await repository.FindOpenAsync(
            1, 1, "12가3456", CancellationToken.None);

        Assert.NotNull(open);
        Assert.Equal(entry.ParkingSessionId, open.PeriodSessionId);
        Assert.Equal("PERIOD-IN.jpg", open.InImage);

        FieldEventResponse exit = await repository.SaveExitAsync(
            new ExitEventRequest(
                Guid.NewGuid(), 1, 20, 201, "12가3456", inDateTime.AddMinutes(30),
                1, 1, null, ParkingEventType.Exit, @"C:\Images\PERIOD-OUT.jpg"),
            open,
            CancellationToken.None);

        Assert.Equal("PERIOD_EXIT_ACCEPTED", exit.ResultCode);
        await using MySqlConnection verifyConnection = new(ConnectionString);
        PeriodExitRow stored = await verifyConnection.QuerySingleAsync<PeriodExitRow>("""
            SELECT outflag OutFlag, inimage InImage, outimage OutImage,
                   parktime ParkTime
            FROM tperiodinout WHERE xindex=@PeriodSessionId;
            """, new { open.PeriodSessionId });
        Assert.Equal("O", stored.OutFlag);
        Assert.Equal("PERIOD-IN.jpg", stored.InImage);
        Assert.Equal("PERIOD-OUT.jpg", stored.OutImage);
        Assert.Equal(30, stored.ParkTime);
    }

    private static async Task ClearAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            DELETE FROM tperiodinout;
            DELETE FROM tperiodmember;
            DELETE FROM parking_event;
            """);
    }

    private sealed class PeriodExitRow
    {
        public string OutFlag { get; set; } = "";
        public string? InImage { get; set; }
        public string? OutImage { get; set; }
        public int ParkTime { get; set; }
    }
}
