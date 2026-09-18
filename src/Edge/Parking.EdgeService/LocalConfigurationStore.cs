using System.Text.Json;
using Microsoft.Data.Sqlite;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class LocalConfigurationStore
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = null };
        public LocalConfigurationStore(string connectionString) { _connectionString = connectionString; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS site_configuration (
                    site_id INTEGER NOT NULL PRIMARY KEY,
                    payload_json TEXT NOT NULL,
                    synced_at_utc TEXT NOT NULL);
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SaveAsync(SiteConfiguration configuration, CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO site_configuration(site_id,payload_json,synced_at_utc)
                VALUES($siteId,$payload,$syncedAt)
                ON CONFLICT(site_id) DO UPDATE SET
                    payload_json=$payload,synced_at_utc=$syncedAt;
                """;
            command.Parameters.AddWithValue("$siteId", configuration.Site.SiteId);
            command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(configuration, _jsonOptions));
            command.Parameters.AddWithValue("$syncedAt", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT payload_json FROM site_configuration WHERE site_id=$siteId;";
            command.Parameters.AddWithValue("$siteId", siteId);
            object? value = await command.ExecuteScalarAsync(cancellationToken);
            return value is string json ? JsonSerializer.Deserialize<SiteConfiguration>(json, _jsonOptions) : null;
        }
    }
}
