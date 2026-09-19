using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed record OutboxMessage(Guid EventId, string EventType, string PayloadJson, int RetryCount);
    internal sealed record OutboxRow(string EventId, string EventType, string PayloadJson, int RetryCount);

    public sealed class SqliteOutboxRepository
    {
        private readonly string _connectionString;
        public SqliteOutboxRepository(string connectionString) { _connectionString = connectionString; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                CREATE TABLE IF NOT EXISTS outbox_message (
                    event_id TEXT NOT NULL PRIMARY KEY,
                    event_type TEXT NOT NULL DEFAULT 'Entry',
                    payload_json TEXT NOT NULL,
                    state INTEGER NOT NULL DEFAULT 0,
                    retry_count INTEGER NOT NULL DEFAULT 0,
                    next_attempt_at_utc TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    completed_at_utc TEXT NULL);
                CREATE INDEX IF NOT EXISTS ix_outbox_pending
                    ON outbox_message(state, next_attempt_at_utc, created_at_utc);
                """;
            await using SqliteConnection connection = new(_connectionString);
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));

            IReadOnlyList<string> columns = (await connection.QueryAsync<string>(
                new CommandDefinition(
                    "SELECT name FROM pragma_table_info('outbox_message');",
                    cancellationToken: cancellationToken))).AsList();

            if (!columns.Contains("event_type", StringComparer.OrdinalIgnoreCase))
                await connection.ExecuteAsync(new CommandDefinition(
                    "ALTER TABLE outbox_message ADD COLUMN event_type TEXT NOT NULL DEFAULT 'Entry';",
                    cancellationToken: cancellationToken));
        }

        public Task EnqueueEntryAsync(FieldEventRequest request, CancellationToken cancellationToken) =>
            EnqueueAsync(request.EventId, "Entry", request, cancellationToken);

        public Task EnqueueExitAsync(ExitEventRequest request, CancellationToken cancellationToken) =>
            EnqueueAsync(request.EventId, "Exit", request, cancellationToken);

        private async Task EnqueueAsync<T>(
            Guid eventId, string eventType, T request, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT OR IGNORE INTO outbox_message
                (event_id,event_type,payload_json,next_attempt_at_utc,created_at_utc)
                VALUES (@EventId,@EventType,@Payload,@Now,@Now);
                """;
            await using SqliteConnection connection = new(_connectionString);
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                EventId = eventId.ToString("D"),
                EventType = eventType,
                Payload = JsonConvert.SerializeObject(request),
                Now = DateTimeOffset.UtcNow.ToString("O")
            }, cancellationToken: cancellationToken));
        }

        public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
            int count, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT event_id EventId,event_type EventType,payload_json PayloadJson,retry_count RetryCount
                FROM outbox_message
                WHERE state=0 AND next_attempt_at_utc<=@Now
                ORDER BY created_at_utc LIMIT @Count;
                """;
            await using SqliteConnection connection = new(_connectionString);
            IEnumerable<OutboxRow> rows = await connection.QueryAsync<OutboxRow>(
                new CommandDefinition(sql, new
                {
                    Now = DateTimeOffset.UtcNow.ToString("O"),
                    Count = count
                }, cancellationToken: cancellationToken));

            return rows.Select(x =>
                new OutboxMessage(Guid.Parse(x.EventId), x.EventType, x.PayloadJson, x.RetryCount)).ToList();
        }

        public Task MarkCompletedAsync(Guid eventId, CancellationToken cancellationToken) =>
            ExecuteAsync(
                "UPDATE outbox_message SET state=1,completed_at_utc=@Now WHERE event_id=@EventId;",
                eventId, DateTimeOffset.UtcNow, cancellationToken);

        public Task MarkFailedAsync(Guid eventId, int retryCount, CancellationToken cancellationToken)
        {
            int seconds = Math.Min(60, Math.Max(1, 1 << Math.Min(retryCount, 6)));
            return ExecuteAsync(
                "UPDATE outbox_message SET retry_count=retry_count+1,next_attempt_at_utc=@Now WHERE event_id=@EventId;",
                eventId, DateTimeOffset.UtcNow.AddSeconds(seconds), cancellationToken);
        }

        private async Task ExecuteAsync(string sql, Guid eventId, DateTimeOffset time, CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                EventId = eventId.ToString("D"),
                Now = time.ToString("O")
            }, cancellationToken: cancellationToken));
        }
    }
}
