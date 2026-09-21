using Dapper;
using Microsoft.Data.Sqlite;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class LocalBootstrapStoreTests
{
    [Fact]
    public async Task 구버전DB를_초기화하면_영상감시폴더열을_추가한다()
    {
        string path = Path.Combine(
            Path.GetTempPath(), $"parking-bootstrap-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={path};Pooling=False";

        try
        {
            await using (SqliteConnection connection = new(connectionString))
            {
                await connection.ExecuteAsync("""
                    CREATE TABLE edge_bootstrap (
                        bootstrap_id INTEGER NOT NULL PRIMARY KEY CHECK(bootstrap_id = 1),
                        site_id INTEGER NOT NULL,
                        central_server_url TEXT NOT NULL,
                        image_server_url TEXT NOT NULL,
                        site_auth_key TEXT NOT NULL,
                        updated_at_utc TEXT NOT NULL);
                    """);
            }

            LocalBootstrapStore store = new(connectionString);

            await store.InitializeAsync(CancellationToken.None);

            await using SqliteConnection verification = new(connectionString);
            IReadOnlyList<string> columns = (await verification.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('edge_bootstrap');")).AsList();
            Assert.Contains("image_watch_path", columns);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
