using Dapper;
using MySqlConnector;
using Parking.Central.Data;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class ParkingCorrectionRepositoryTests
{
    private const long SiteId = 990102;
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 차량번호는_I상태에서만_변경한다()
    {
        await using MySqlConnection connection = new(_connectionString);
        await ClearAsync(connection);
        try
        {
            await connection.ExecuteAsync("""
                INSERT INTO tparkinfo
                (xindex,sitenum,groupnum,ineventid,carnum,cartype,inlaneid,indevicenum,indate,outflag) VALUES
                (99010201,@SiteId,2,'99010200000000000000000000000001','11가1111',1,1,1,NOW(),'I'),
                (99010202,@SiteId,2,'99010200000000000000000000000002','22나2222',1,1,1,NOW(),'X');
                """, new { SiteId });
            ParkingCorrectionRepository repository = new(_connectionString);

            Assert.True((await repository.CorrectCarNumberAsync(
                99010201, "99가9999", CancellationToken.None)).Updated);
            Assert.False((await repository.CorrectCarNumberAsync(
                99010202, "88나8888", CancellationToken.None)).Updated);
        }
        finally { await ClearAsync(connection); }
    }

    private static Task<int> ClearAsync(MySqlConnection connection) =>
        connection.ExecuteAsync("DELETE FROM tparkinfo WHERE sitenum=@SiteId;", new { SiteId });
}
