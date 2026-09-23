using Dapper;
using Microsoft.Data.Sqlite;
using Parking.Contracts;

namespace Parking.EdgeService;

public sealed class LocalBootstrapStore
{
    private readonly string _connectionString;

    public LocalBootstrapStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS edge_bootstrap (
                bootstrap_id INTEGER NOT NULL PRIMARY KEY CHECK(bootstrap_id = 1),
                site_id INTEGER NOT NULL,
                central_server_url TEXT NOT NULL,
                parking_api_url TEXT NOT NULL DEFAULT 'http://localhost:5000/',
                image_server_url TEXT NOT NULL,
                image_watch_path TEXT NOT NULL DEFAULT '',
                site_auth_key TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL);
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));
        IEnumerable<string> columns = await connection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT name FROM pragma_table_info('edge_bootstrap');",
                cancellationToken: cancellationToken));
        if (!columns.Any(x => string.Equals(x, "image_watch_path", StringComparison.OrdinalIgnoreCase)))
            await connection.ExecuteAsync(new CommandDefinition(
                "ALTER TABLE edge_bootstrap ADD COLUMN image_watch_path TEXT NOT NULL DEFAULT '';",
                cancellationToken: cancellationToken));
        if (!columns.Any(x => string.Equals(x, "parking_api_url", StringComparison.OrdinalIgnoreCase)))
            await connection.ExecuteAsync(new CommandDefinition(
                "ALTER TABLE edge_bootstrap ADD COLUMN parking_api_url TEXT NOT NULL DEFAULT 'http://localhost:5000/';",
                cancellationToken: cancellationToken));
    }

    public async Task<EdgeBootstrapSettings?> GetAsync(
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT site_id SiteId,
                   central_server_url CentralServerUrl,
                   parking_api_url ParkingApiUrl,
                   image_server_url ImageServerUrl,
                   image_watch_path ImageWatchPath,
                   site_auth_key SiteAuthKey,
                   updated_at_utc UpdatedAtUtc
            FROM edge_bootstrap
            WHERE bootstrap_id=1;
            """;

        await using SqliteConnection connection = new(_connectionString);
        BootstrapRow? row = await connection.QuerySingleOrDefaultAsync<BootstrapRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return row is null
            ? null
            : new EdgeBootstrapSettings(
                row.SiteId,
                row.CentralServerUrl,
                row.ParkingApiUrl,
                row.ImageServerUrl,
                row.ImageWatchPath,
                row.SiteAuthKey,
                DateTimeOffset.Parse(
                    row.UpdatedAtUtc,
                    System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task SaveAsync(
        EdgeBootstrapSettings settings,
        CancellationToken cancellationToken)
    {
        Validate(settings);

        const string sql = """
            INSERT INTO edge_bootstrap(
                bootstrap_id,site_id,central_server_url,parking_api_url,image_server_url,image_watch_path,
                site_auth_key,updated_at_utc)
            VALUES(1,@SiteId,@CentralServerUrl,@ParkingApiUrl,@ImageServerUrl,@ImageWatchPath,@SiteAuthKey,@UpdatedAtUtc)
            ON CONFLICT(bootstrap_id) DO UPDATE SET
                site_id=@SiteId,
                central_server_url=@CentralServerUrl,
                parking_api_url=@ParkingApiUrl,
                image_server_url=@ImageServerUrl,
                image_watch_path=@ImageWatchPath,
                site_auth_key=@SiteAuthKey,
                updated_at_utc=@UpdatedAtUtc;
            """;

        await using SqliteConnection connection = new(_connectionString);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            settings.SiteId,
            CentralServerUrl = NormalizeUrl(settings.CentralServerUrl),
            ParkingApiUrl = NormalizeUrl(settings.ParkingApiUrl),
            ImageServerUrl = NormalizeUrl(settings.ImageServerUrl),
            ImageWatchPath = settings.ImageWatchPath.Trim(),
            SiteAuthKey = settings.SiteAuthKey.Trim(),
            UpdatedAtUtc = settings.UpdatedAtUtc.ToUniversalTime().ToString("O")
        }, cancellationToken: cancellationToken));
    }

    private static void Validate(EdgeBootstrapSettings settings)
    {
        if (settings.SiteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(settings.SiteId));
        ValidateUrl(settings.CentralServerUrl, nameof(settings.CentralServerUrl));
        ValidateUrl(settings.ParkingApiUrl, nameof(settings.ParkingApiUrl));
        ValidateUrl(settings.ImageServerUrl, nameof(settings.ImageServerUrl));
        if (string.IsNullOrWhiteSpace(settings.ImageWatchPath))
            throw new ArgumentException("영상 감시폴더가 필요합니다.", nameof(settings.ImageWatchPath));
        if (string.IsNullOrWhiteSpace(settings.SiteAuthKey))
            throw new ArgumentException("현장 인증키가 필요합니다.", nameof(settings.SiteAuthKey));
    }

    private static void ValidateUrl(string value, string parameterName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("HTTP 또는 HTTPS 서버 주소가 필요합니다.", parameterName);
    }

    private static string NormalizeUrl(string value) =>
        value.Trim().TrimEnd('/') + "/";

    private sealed record BootstrapRow(
        long SiteId,
        string CentralServerUrl,
        string ParkingApiUrl,
        string ImageServerUrl,
        string ImageWatchPath,
        string SiteAuthKey,
        string UpdatedAtUtc);
}
