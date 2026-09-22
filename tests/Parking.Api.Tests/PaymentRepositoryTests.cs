using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
[Trait("Category", "DatabaseMutation")]
public sealed class PaymentRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_RUNTIME_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_RUNTIME_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 같은_PaymentId는_결제내역을_중복생성하지_않는다()
    {
        long parkingSessionId = await CreateSessionAsync();
        CompletePaymentRequest request = new()
        {
            PaymentId = Guid.NewGuid(),
            ParkingSessionId = parkingSessionId,
            SiteId = 9001,
            OriginalFee = 1_000,
            DiscountFee = 400,
            PaidAmount = 600,
            PaymentMethod = "Card",
            ApprovalNumber = "12345678",
            TerminalId = "KIOSK-01",
            PaidAt = DateTimeOffset.UtcNow
        };
        PaymentRepository repository = new(ConnectionString);

        PaymentCompleteResponse first = await repository.CompleteAsync(request, CancellationToken.None);
        await using (MySqlConnection mutableSession = new(ConnectionString))
        {
            await mutableSession.ExecuteAsync("""
                UPDATE tparkinfo
                SET parkfee=2000,discountfee=500,payfee=1500
                WHERE xindex=@ParkingSessionId;
                """, new { ParkingSessionId = parkingSessionId });
        }
        PaymentCompleteResponse second = await repository.CompleteAsync(request, CancellationToken.None);

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Equal(1, await GetPaymentCountAsync(parkingSessionId));
        Assert.Equal("X", await GetSessionOutFlagAsync(parkingSessionId));
        await using MySqlConnection connection = new(ConnectionString);
        PaymentStorageRow stored = await connection.QuerySingleAsync<PaymentStorageRow>("""
            SELECT money Money,dealtype DealType,acceptnum ApprovalNumber
            FROM tbcardinfo WHERE pindex=@ParkingSessionId;
            """, new { ParkingSessionId = parkingSessionId });
        Assert.Equal(600, stored.Money);
        Assert.Equal("APPROVE", stored.DealType);
        Assert.Equal("12345678", stored.ApprovalNumber);
    }

    private static async Task<long> CreateSessionAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO tparkinfo
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indevicenum,indate,outflag)
            VALUES (9001,@EntryEventId,'12가3456',2,1,9010,401,UTC_TIMESTAMP(),'I');
            SELECT LAST_INSERT_ID();
            """, new { EntryEventId = Guid.NewGuid().ToString("N") });
    }

    private static async Task<long> GetPaymentCountAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM tbcardinfo WHERE pindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }

    private static async Task<string> GetSessionOutFlagAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM tparkinfo WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }

    private sealed class PaymentStorageRow
    {
        public long Money { get; set; }
        public string DealType { get; set; } = "";
        public string ApprovalNumber { get; set; } = "";
    }
}
