using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed record PendingKioskEvent(
    Guid EventId,
    long KioskDeviceId,
    LprRecognition Recognition,
    string State,
    DateTimeOffset CreatedAt);

public sealed class KioskPendingRepository
{
    private readonly string _connectionString;
    public KioskPendingRepository(string connectionString) { _connectionString = connectionString; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS kiosk_pending_event (
                event_id TEXT NOT NULL PRIMARY KEY,
                kiosk_device_id INTEGER NOT NULL,
                recognition_json TEXT NOT NULL,
                state TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                completed_at_utc TEXT NULL);
            CREATE INDEX IF NOT EXISTS ix_kiosk_pending_device
                ON kiosk_pending_event(kiosk_device_id,state,created_at_utc);
            """;
        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task EnqueueAsync(
        long kioskDeviceId, LprRecognition recognition, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT OR IGNORE INTO kiosk_pending_event
            (event_id,kiosk_device_id,recognition_json,state,created_at_utc)
            VALUES(@EventId,@KioskDeviceId,@RecognitionJson,'Pending',@CreatedAt);
            """;
        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            EventId = recognition.EventId.ToString("N"),
            KioskDeviceId = kioskDeviceId,
            RecognitionJson = JsonConvert.SerializeObject(recognition),
            CreatedAt = DateTimeOffset.UtcNow.ToString("O")
        }, cancellationToken: cancellationToken));
    }

    public async Task<PendingKioskEvent?> GetAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        Row? row = await connection.QuerySingleOrDefaultAsync<Row>(new CommandDefinition("""
            SELECT event_id EventId,kiosk_device_id KioskDeviceId,recognition_json RecognitionJson,
                   state State,created_at_utc CreatedAt
            FROM kiosk_pending_event WHERE event_id=@EventId;
            """, new { EventId = eventId.ToString("N") }, cancellationToken: cancellationToken));
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<PendingKioskEvent>> GetPendingAsync(
        long kioskDeviceId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        IEnumerable<Row> rows = await connection.QueryAsync<Row>(new CommandDefinition("""
            SELECT event_id EventId,kiosk_device_id KioskDeviceId,recognition_json RecognitionJson,
                   state State,created_at_utc CreatedAt
            FROM kiosk_pending_event
            WHERE kiosk_device_id=@KioskDeviceId AND state IN ('Pending','Notified')
            ORDER BY created_at_utc;
            """, new { KioskDeviceId = kioskDeviceId }, cancellationToken: cancellationToken));
        return rows.Select(Map).ToArray();
    }

    public Task MarkNotifiedAsync(Guid eventId, CancellationToken cancellationToken) =>
        SetStateAsync(eventId, "Notified", false, cancellationToken);
    public Task MarkCompletedAsync(Guid eventId, CancellationToken cancellationToken) =>
        SetStateAsync(eventId, "Completed", true, cancellationToken);
    public Task MarkOfflineOpenedAsync(Guid eventId, CancellationToken cancellationToken) =>
        SetStateAsync(eventId, "OfflineOpened", true, cancellationToken);

    public async Task<bool> TryBeginCompletionAsync(
        Guid eventId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        int updated = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE kiosk_pending_event SET state='Processing'
            WHERE event_id=@EventId AND state IN ('Pending','Notified');
            """, new { EventId = eventId.ToString("N") }, cancellationToken: cancellationToken));
        return updated == 1;
    }

    public async Task<bool> TryBeginOfflineOpenAsync(
        Guid eventId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        int updated = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE kiosk_pending_event SET state='OfflineOpening'
            WHERE event_id=@EventId AND state IN ('Pending','Notified');
            """, new { EventId = eventId.ToString("N") }, cancellationToken: cancellationToken));
        return updated == 1;
    }

    public Task ReleaseCompletionAsync(Guid eventId, CancellationToken cancellationToken) =>
        SetStateAsync(eventId, "Notified", false, cancellationToken);

    private async Task SetStateAsync(
        Guid eventId, string state, bool completed, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE kiosk_pending_event SET state=@State,
                completed_at_utc=CASE WHEN @Completed=1 THEN @Now ELSE completed_at_utc END
            WHERE event_id=@EventId;
            """, new
        {
            EventId = eventId.ToString("N"), State = state,
            Completed = completed ? 1 : 0, Now = DateTimeOffset.UtcNow.ToString("O")
        }, cancellationToken: cancellationToken));
    }

    private static PendingKioskEvent Map(Row row) => new(
        Guid.ParseExact(row.EventId, "N"), row.KioskDeviceId,
        JsonConvert.DeserializeObject<LprRecognition>(row.RecognitionJson)!,
        row.State, DateTimeOffset.Parse(row.CreatedAt));

    private sealed record Row(
        string EventId, long KioskDeviceId, string RecognitionJson, string State, string CreatedAt);
}
