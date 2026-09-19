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

    private static async Task<string> GetOutFlagAsync(long parkingSessionId)
    {
        await using MySqlConnection connection = new(ConnectionString);
        return await connection.ExecuteScalarAsync<string>(
            "SELECT outflag FROM parking_session WHERE xindex=@ParkingSessionId;",
            new { ParkingSessionId = parkingSessionId });
    }
}
