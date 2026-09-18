using System.Text.Json;
using Microsoft.Data.Sqlite;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed record OutboxMessage(Guid EventId, string EventType, string PayloadJson, int RetryCount);

    public sealed class SqliteOutboxRepository
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = null };
        public SqliteOutboxRepository(string connectionString) { _connectionString = connectionString; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
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
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            bool hasEventType = false;
            await using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(outbox_message);";
                await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    if (string.Equals(reader.GetString(1), "event_type", StringComparison.OrdinalIgnoreCase))
                        hasEventType = true;
            }
            if (!hasEventType)
            {
                await using SqliteCommand command = connection.CreateCommand();
                command.CommandText = "ALTER TABLE outbox_message ADD COLUMN event_type TEXT NOT NULL DEFAULT 'Entry';";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public Task EnqueueEntryAsync(FieldEventRequest request, CancellationToken cancellationToken) =>
            EnqueueAsync(request.EventId, "Entry", request, cancellationToken);

        public Task EnqueueExitAsync(ExitEventRequest request, CancellationToken cancellationToken) =>
            EnqueueAsync(request.EventId, "Exit", request, cancellationToken);

        private async Task EnqueueAsync<T>(Guid eventId, string eventType, T request, CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT OR IGNORE INTO outbox_message
                (event_id,event_type,payload_json,next_attempt_at_utc,created_at_utc)
                VALUES ($eventId,$eventType,$payload,$now,$now);
                """;
            string now = DateTimeOffset.UtcNow.ToString("O");
            command.Parameters.AddWithValue("$eventId", eventId.ToString("D"));
            command.Parameters.AddWithValue("$eventType", eventType);
            command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(request, _jsonOptions));
            command.Parameters.AddWithValue("$now", now);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int count, CancellationToken cancellationToken)
        {
            List<OutboxMessage> messages = new();
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT event_id,event_type,payload_json,retry_count FROM outbox_message
                WHERE state=0 AND next_attempt_at_utc<=$now
                ORDER BY created_at_utc LIMIT $count;
                """;
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("$count", count);
            await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                messages.Add(new OutboxMessage(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.GetInt32(3)));
            return messages;
        }

        public Task MarkCompletedAsync(Guid eventId, CancellationToken cancellationToken) =>
            ExecuteAsync("UPDATE outbox_message SET state=1,completed_at_utc=$now WHERE event_id=$eventId;",
                eventId, DateTimeOffset.UtcNow, cancellationToken);

        public Task MarkFailedAsync(Guid eventId, int retryCount, CancellationToken cancellationToken)
        {
            int seconds = Math.Min(60, Math.Max(1, 1 << Math.Min(retryCount, 6)));
            return ExecuteAsync("UPDATE outbox_message SET retry_count=retry_count+1,next_attempt_at_utc=$now WHERE event_id=$eventId;",
                eventId, DateTimeOffset.UtcNow.AddSeconds(seconds), cancellationToken);
        }

        private async Task ExecuteAsync(string sql, Guid eventId, DateTimeOffset time, CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("$eventId", eventId.ToString("D"));
            command.Parameters.AddWithValue("$now", time.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
