namespace Parking.Api.Tests;

public sealed class FullSchemaDeviceSeedTests
{
    [Fact]
    public void Full_schema_contains_site_9001_device_configuration()
    {
        string sql = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "database", "mysql", "000_full_schema.sql"));

        Assert.Contains("(2001,9001,9020,201,'KIOSK'", sql);
        Assert.Contains("(4001,9001,9010,401,'LPR'", sql);
        Assert.Contains("(4002,9001,9020,402,'LPR'", sql);
        Assert.Contains("(4003,9001,9010,403,'LPR'", sql);
        Assert.Contains("(4004,9001,9020,404,'LPR'", sql);
        Assert.Contains("(5001,9001,9020,501,'LDM'", sql);
        Assert.Contains("(9001,2001,5001,'LDM',1)", sql);
        Assert.Contains("(9001,4002,2001,'KIOSK',1)", sql);
        Assert.Contains("(9001,4002,5001,'LDM',1)", sql);
        Assert.DoesNotContain("(9402,", sql);
        Assert.DoesNotContain("(9403,", sql);
        Assert.DoesNotContain("(9404,", sql);
        Assert.Contains("(9001,2,1,0,1,1,30,0,1)", sql);
        Assert.Contains("(9001,2,'CMD_KIOSK_OFFLINE_POLICY','OPEN'", sql);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ParkingSystem.sln")))
            current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("ParkingSystem.sln");
    }
}
