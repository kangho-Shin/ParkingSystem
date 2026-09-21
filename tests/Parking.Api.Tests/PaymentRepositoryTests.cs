using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

[Collection("Database")]
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
        PaymentCompleteResponse second = await repository.CompleteAsync(request, CancellationToken.None);

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Equal(1, await GetPaymentCountAsync(parkingSessionId));
        Assert.Equal("X", await GetSessionOutFlagAsync(parkingSessionId));
    }

    private static async Task<long> CreateSessionAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (sitenum,ineventid,carnum,groupnum,cartype,inlaneid,indate,outflag)
            VALUES (9001,@EntryEventId,'12가3456',2,1,9010,UTC_TIMESTAMP(6),'I');
            SELECT LAST_INSERT_ID();
            """, new { EntryEventId = Guid.NewGuid().ToByteArray() });
    }

    private static async Task<long> GetPaymentCountAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM payment WHERE parkindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }

    private static async Task<string> GetSessionOutFlagAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }
}
