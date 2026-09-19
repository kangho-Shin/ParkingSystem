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
    public async Task 미결제_차량은_출차를_거부한다()
    {
        string carNumber = $"TEST{Guid.NewGuid():N}"[..20];
        long parkingSessionId = await CreateSessionAsync(carNumber, "Entered");
        ExitEventRequest request = new(
            Guid.NewGuid(),
            1,
            20,
            201,
            carNumber,
            DateTimeOffset.UtcNow);
        ParkingExitRepository repository = new(ConnectionString);

        FieldEventResponse result = await repository.SaveExitAsync(
            request,
            CancellationToken.None);

        Assert.False(result.Accepted);
        Assert.False(result.OpenBarrier);
        Assert.Equal("PAYMENT_REQUIRED", result.ResultCode);
        Assert.Equal("Entered", await GetSessionStatusAsync(parkingSessionId));
    }

    private static async Task<long> CreateSessionAsync(string carNumber, string status)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<long>("""
            INSERT INTO parking_session
            (site_id,entry_event_id,car_number,entry_lane_id,entry_at_utc,status)
            VALUES (1,@EntryEventId,@CarNumber,10,UTC_TIMESTAMP(6),@Status);
            SELECT LAST_INSERT_ID();
            """, new
            {
                EntryEventId = Guid.NewGuid().ToByteArray(),
                CarNumber = carNumber,
                Status = status
            });
    }

    private static async Task<string> GetSessionStatusAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT status FROM parking_session WHERE parking_session_id=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }
}
