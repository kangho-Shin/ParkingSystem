using MySqlConnector;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public class ParkingEventRepositoryTests
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("PARKING_TEST_CONNECTION")
        ?? throw new InvalidOperationException(
            "PARKING_TEST_CONNECTION 환경변수가 없습니다.");

    [Fact]
    public async Task 같은_EventId는_주차내역을_중복생성하지_않는다()
    {
        await ClearTablesAsync();

        FieldEventRequest request = new(
            Guid.NewGuid(),
            1,
            10,
            101,
            "12가3456",
            DateTimeOffset.UtcNow);

        ParkingEventRepository repository = new(ConnectionString);

        FieldEventResponse first =
            await repository.SaveEntryAsync(request, CancellationToken.None);

        FieldEventResponse second =
            await repository.SaveEntryAsync(request, CancellationToken.None);

        Assert.Equal(first.ParkingSessionId, second.ParkingSessionId);
        Assert.Equal(1, await GetSessionCountAsync());
    }

    private static async Task ClearTablesAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText =
            "DELETE FROM parking_session; DELETE FROM parking_event;";

        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> GetSessionCountAsync()
    {
        await using MySqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();

        await using MySqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM parking_session;";

        object? value = await command.ExecuteScalarAsync();
        return Convert.ToInt64(value);
    }
}
