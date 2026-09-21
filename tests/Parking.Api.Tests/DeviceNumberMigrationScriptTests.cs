namespace Parking.Api.Tests;

public sealed class DeviceNumberMigrationScriptTests
{
    [Fact]
    public void Migration_contains_every_device_and_reference_mapping()
    {
        string sql = ReadMigration();
        foreach (string mapping in new[] { "9301, 2001, 201", "9101, 4001, 401", "9201, 4002, 402", "9401, 5001, 501" })
            Assert.Contains(mapping, sql, StringComparison.OrdinalIgnoreCase);
        foreach (string auxiliary in new[] { "4003,9001,9010,403", "4004,9001,9020,404" })
            Assert.Contains(auxiliary, sql, StringComparison.OrdinalIgnoreCase);
        foreach (string reference in new[]
        {
            "parking_event", "parking_session", "tperiodinout",
            "parking_device_link", "parking_site_sync"
        }) Assert.Contains(reference, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_is_idempotent_and_has_no_table_recreation()
    {
        string sql = ReadMigration();
        string executableSql = sql.Replace("''", "'", StringComparison.Ordinal);
        Assert.Contains("ON DUPLICATE KEY UPDATE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("START TRANSACTION", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SIGNAL SQLSTATE '45000'", executableSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("information_schema.tables", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("information_schema.columns", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("column_name='indevicenum'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("column_name='outdevicenum'", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LEFT JOIN device_id_map source_map", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LEFT JOIN device_id_map target_map", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadMigration()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ParkingSystem.sln")))
            current = current.Parent;
        return File.ReadAllText(Path.Combine(current!.FullName, "database", "mysql", "012_device_number_ranges.sql"));
    }
}
