using Dapper;
using Microsoft.Data.Sqlite;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class LocalBootstrapStoreTests
{
    [Fact]
    public async Task 구버전DB를_초기화하면_신규설정열과_ParkingApi기본주소를_추가한다()
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
                await connection.ExecuteAsync("""
                    INSERT INTO edge_bootstrap(
                        bootstrap_id, site_id, central_server_url, image_server_url,
                        site_auth_key, updated_at_utc)
                    VALUES(1, 9001, 'http://localhost:5100/', 'http://localhost:5400/',
                        'test-key', '2026-09-23T00:00:00.0000000+00:00');
                    """);
            }

            LocalBootstrapStore store = new(connectionString);

            await store.InitializeAsync(CancellationToken.None);

            await using SqliteConnection verification = new(connectionString);
            IReadOnlyList<string> columns = (await verification.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('edge_bootstrap');")).AsList();
            Assert.Contains("image_watch_path", columns);
            Assert.Contains("parking_api_url", columns);

            Parking.Contracts.EdgeBootstrapSettings? settings =
                await store.GetAsync(CancellationToken.None);
            Assert.NotNull(settings);
            Assert.Equal("http://localhost:5000/", settings.ParkingApiUrl);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
