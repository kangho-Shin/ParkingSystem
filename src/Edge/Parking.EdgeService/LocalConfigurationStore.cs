using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Parking.Contracts;

namespace Parking.EdgeService
{
    public sealed class LocalConfigurationStore
    {
        private readonly string _connectionString;
        public LocalConfigurationStore(string connectionString) { _connectionString = connectionString; }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                CREATE TABLE IF NOT EXISTS site_configuration (
                    site_id INTEGER NOT NULL PRIMARY KEY,
                    payload_json TEXT NOT NULL,
                    synced_at_utc TEXT NOT NULL);
                """;
            await using SqliteConnection connection = new(_connectionString);
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        }

        public async Task SaveAsync(SiteConfiguration configuration, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO site_configuration(site_id,payload_json,synced_at_utc)
                VALUES(@SiteId,@Payload,@SyncedAt)
                ON CONFLICT(site_id) DO UPDATE SET
                    payload_json=@Payload,synced_at_utc=@SyncedAt;
                """;
            await using SqliteConnection connection = new(_connectionString);
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                SiteId = configuration.Site.SiteId,
                Payload = JsonConvert.SerializeObject(configuration),
                SyncedAt = DateTimeOffset.UtcNow.ToString("O")
            }, cancellationToken: cancellationToken));
        }

        public async Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken cancellationToken)
        {
            await using SqliteConnection connection = new(_connectionString);
            string? json = await connection.QuerySingleOrDefaultAsync<string>(
                new CommandDefinition(
                    "SELECT payload_json FROM site_configuration WHERE site_id=@SiteId;",
                    new { SiteId = siteId },
                    cancellationToken: cancellationToken));
            return json is null ? null : JsonConvert.DeserializeObject<SiteConfiguration>(json);
        }
    }
}
