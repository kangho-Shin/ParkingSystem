using Newtonsoft.Json;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

public sealed class ParkingEventRepository : IParkingEventRepository
{
    private readonly string _connectionString;

    public ParkingEventRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<FieldEventResponse> SaveEntryAsync(
        FieldEventRequest request,
        CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        try {
            byte[] eventId = request.EventId.ToByteArray();

            const string insertEventSql = """
                INSERT IGNORE INTO parking_event
                (event_id, site_id, lane_id, device_id, event_type,
                 car_number, occurred_at_utc)
                VALUES
                (@EventId, @SiteId, @LaneId, @DeviceId, 'Entry',
                 @CarNumber, @OccurredAtUtc);
                """;

            int inserted = await connection.ExecuteAsync(
                new CommandDefinition(
                    insertEventSql,
                    new
                    {
                        EventId = eventId,
                        request.SiteId,
                        request.LaneId,
                        request.DeviceId,
                        request.CarNumber,
                        OccurredAtUtc = request.OccurredAt.UtcDateTime
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (inserted == 0) {
                const string resultSql =
                    "SELECT result_json FROM parking_event WHERE event_id=@EventId;";

                string resultJson = await connection.QuerySingleAsync<string>(
                    new CommandDefinition(
                        resultSql,
                        new { EventId = eventId },
                        transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return JsonConvert.DeserializeObject<FieldEventResponse>(resultJson)
                       ?? throw new InvalidOperationException("기존 처리결과를 읽지 못했습니다.");
            }

            const string insertSessionSql = """
                INSERT INTO parking_session
                (site_id, entry_event_id, car_number, entry_lane_id,
                 entry_at_utc, status)
                VALUES
                (@SiteId, @EventId, @CarNumber, @LaneId,
                 @OccurredAtUtc, 'Entered');

                SELECT LAST_INSERT_ID();
                """;

            long parkingSessionId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    insertSessionSql,
                    new
                    {
                        request.SiteId,
                        EventId = eventId,
                        request.CarNumber,
                        request.LaneId,
                        OccurredAtUtc = request.OccurredAt.UtcDateTime
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            FieldEventResponse response = new(
                request.EventId,
                true,
                parkingSessionId,
                "ENTRY_ACCEPTED",
                "입차되었습니다.",
                true);

            const string updateResultSql = """
                UPDATE parking_event
                SET result_json=@ResultJson
                WHERE event_id=@EventId;
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    updateResultSql,
                    new
                    {
                        EventId = eventId,
                        ResultJson = JsonConvert.SerializeObject(response)
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}