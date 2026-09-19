using Dapper;
using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public sealed class PaymentRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException("PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 같은_PaymentId는_결제내역을_중복생성하지_않는다()
    {
        long parkingSessionId = await CreateSessionAsync();
        CompletePaymentRequest request = new()
        {
            PaymentId = Guid.NewGuid(),
            ParkingSessionId = parkingSessionId,
            SiteId = 1,
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
        Assert.Equal("Paid", await GetSessionStatusAsync(parkingSessionId));
    }

    private static async Task<long> CreateSessionAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (site_id,entry_event_id,car_number,entry_lane_id,entry_at_utc,status)
            VALUES (1,@EntryEventId,'12가3456',10,UTC_TIMESTAMP(6),'Entered');
            SELECT LAST_INSERT_ID();
            """, new { EntryEventId = Guid.NewGuid().ToByteArray() });
    }

    private static async Task<long> GetPaymentCountAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM payment WHERE parking_session_id=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }

    private static async Task<string> GetSessionStatusAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM parking_session WHERE parking_session_id=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }
}
