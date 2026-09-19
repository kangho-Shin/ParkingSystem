using Newtonsoft.Json;
using Dapper;
using MySqlConnector;
using Parking.Contracts;

namespace Parking.Central.Data;

internal sealed class OpenParkingSessionRow
{
    public long ParkingSessionId { get; set; }
    public long SiteId { get; set; }
    public string CarNumber { get; set; } = "";
    public long EntryLaneId { get; set; }
    public DateTime EntryAt { get; set; }
    public string Status { get; set; } = "";
}

public sealed class ParkingExitRepository : IParkingExitRepository
{
    private readonly string _connectionString;
    public ParkingExitRepository(string connectionString) { _connectionString = connectionString; }

    public async Task<OpenParkingSessionResponse?> FindOpenAsync(long siteId, string carNumber, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT parking_session_id ParkingSessionId, site_id SiteId, car_number CarNumber,
                   entry_lane_id EntryLaneId, entry_at_utc EntryAt, status Status
            FROM parking_session
            WHERE site_id=@SiteId AND car_number=@CarNumber AND status<>'Exited'
            ORDER BY entry_at_utc DESC LIMIT 1;
            """;
        await using MySqlConnection connection = new(_connectionString);
        OpenParkingSessionRow? row = await connection.QuerySingleOrDefaultAsync<OpenParkingSessionRow>(
            new CommandDefinition(
                sql,
                new { SiteId = siteId, CarNumber = carNumber },
                cancellationToken: cancellationToken));

        if (row is null)
            return null;

        DateTime entryAtUtc = DateTime.SpecifyKind(row.EntryAt, DateTimeKind.Utc);
        return new OpenParkingSessionResponse(
            row.ParkingSessionId,
            row.SiteId,
            row.CarNumber,
            row.EntryLaneId,
            new DateTimeOffset(entryAtUtc),
            row.Status);
    }

    public async Task<FieldEventResponse> SaveExitAsync(ExitEventRequest request, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        byte[] eventId = request.EventId.ToByteArray();

        try
        {
            const string insertEvent = """
                INSERT IGNORE INTO parking_event
                (event_id,site_id,lane_id,device_id,event_type,car_number,occurred_at_utc)
                VALUES (@EventId,@SiteId,@LaneId,@DeviceId,'Exit',@CarNumber,@OccurredAtUtc);
                """;
            int inserted = await connection.ExecuteAsync(new CommandDefinition(insertEvent, new
            {
                EventId = eventId, request.SiteId, request.LaneId, request.DeviceId,
                request.CarNumber, OccurredAtUtc = request.OccurredAt.UtcDateTime
            }, transaction, cancellationToken: cancellationToken));

            if (inserted == 0)
            {
                string json = await connection.QuerySingleAsync<string>(new CommandDefinition(
                    "SELECT result_json FROM parking_event WHERE event_id=@EventId;",
                    new { EventId = eventId }, transaction, cancellationToken: cancellationToken));
                await transaction.CommitAsync(cancellationToken);
                return JsonConvert.DeserializeObject<FieldEventResponse>(json)
                    ?? throw new InvalidOperationException("기존 출차결과를 읽지 못했습니다.");
            }

            long? sessionId = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition("""
                SELECT parking_session_id FROM parking_session
                WHERE site_id=@SiteId AND car_number=@CarNumber AND status<>'Exited'
                ORDER BY entry_at_utc DESC LIMIT 1 FOR UPDATE;
                """, new { request.SiteId, request.CarNumber }, transaction, cancellationToken: cancellationToken));

            FieldEventResponse result;
            if (sessionId is null)
            {
                result = new FieldEventResponse(request.EventId, false, null, "OPEN_SESSION_NOT_FOUND", "미출차 차량이 없습니다.", false);
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    UPDATE parking_session SET exit_event_id=@EventId, exit_lane_id=@LaneId,
                    exit_at_utc=@OccurredAtUtc, status='Exited'
                    WHERE parking_session_id=@ParkingSessionId;
                    """, new { EventId = eventId, request.LaneId, OccurredAtUtc = request.OccurredAt.UtcDateTime, ParkingSessionId = sessionId.Value },
                    transaction, cancellationToken: cancellationToken));
                result = new FieldEventResponse(request.EventId, true, sessionId, "EXIT_ACCEPTED", "출차되었습니다.", true);
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE parking_event SET result_json=@ResultJson WHERE event_id=@EventId;",
                new { EventId = eventId, ResultJson = JsonConvert.SerializeObject(result) },
                transaction, cancellationToken: cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
