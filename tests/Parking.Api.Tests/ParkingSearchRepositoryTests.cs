using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class ParkingSearchRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 뒤4자리_조회는_같은그룹_미출차차량을_최신순으로_반환한다()
    {
        await ClearTablesAsync();
        DateTime baseTime = ParkingLocalTime.ToDatabase(DateTimeOffset.UtcNow.AddHours(-3));
        long firstId = await CreateSessionAsync("12가3456", 2, "IN-1.jpg", baseTime, "I");
        long secondId = await CreateSessionAsync("34나3456", 2, "IN-2.jpg", baseTime.AddHours(1), "I");
        await CreateSessionAsync("56다3456", 3, "OTHER-GROUP.jpg", baseTime.AddHours(2), "I");
        await CreateSessionAsync("78라3456", 2, "EXITED.jpg", baseTime.AddHours(2), "O");
        ParkingSearchRepository repository = new(ConnectionString);

        IReadOnlyList<ParkingSearchCandidate> suffixResult = await repository.SearchAsync(
            9001, 2, "3456", CancellationToken.None);
        IReadOnlyList<ParkingSearchCandidate> exactResult = await repository.SearchAsync(
            9001, 2, "12가3456", CancellationToken.None);

        Assert.Equal([secondId, firstId], suffixResult.Select(x => x.ParkingSessionId));
        Assert.Equal("IN-2.jpg", suffixResult[0].InImage);
        Assert.Single(exactResult);
        Assert.Equal(firstId, exactResult[0].ParkingSessionId);
        Assert.Equal(2, await CountInProgressAsync([firstId, secondId]));
    }

    private static async Task ClearTablesAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.ExecuteAsync("""
            DELETE FROM tdiscountinfo;
            DELETE FROM tbcardinfo;
            DELETE FROM tparkinfo;
            DELETE FROM tparkevent;
            """);
    }

    private static async Task<long> CreateSessionAsync(
        string carNumber,
        int groupnum,
        string inImage,
        DateTime inDateTime,
        string outFlag)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO tparkinfo
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,
             indate,inimage,outflag)
            VALUES
            (9001,@EventId,@CarNumber,@Groupnum,1,9010,401,@InDateTime,@InImage,@OutFlag);
            SELECT LAST_INSERT_ID();
            """, new
        {
            EventId = Guid.NewGuid().ToString("N"),
            CarNumber = carNumber,
            Groupnum = groupnum,
            InDateTime = inDateTime,
            InImage = inImage,
            OutFlag = outFlag
        });
    }

    private static async Task<long> CountInProgressAsync(long[] parkingSessionIds)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM tparkinfo WHERE xindex IN @ParkingSessionIds AND outflag='I';",
            new { ParkingSessionIds = parkingSessionIds });
    }
}
