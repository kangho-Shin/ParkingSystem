namespace Parking.Api.Tests;

public sealed class ServerBatchScriptTests
{
    [Theory]
    [InlineData("start-parking-api.bat", "http://localhost:5000", "src\\Central\\Parking.Api\\Parking.Api.csproj")]
    [InlineData("start-parking-edgegateway.bat", "http://localhost:5100", "src\\Central\\Parking.EdgeGateway\\Parking.EdgeGateway.csproj")]
    [InlineData("start-parking-edgeservice.bat", "http://localhost:5200", "src\\Edge\\Parking.EdgeService\\Parking.EdgeService.csproj")]
    public void Individual_server_script_runs_existing_build(
        string fileName,
        string url,
        string project)
    {
        string script = ReadRootFile(fileName);

        Assert.Contains("dotnet run --no-build --no-launch-profile", script);
        Assert.Contains(url, script);
        Assert.Contains(project, script);
        Assert.DoesNotContain("dotnet build", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Combined_script_starts_all_three_server_scripts()
    {
        string script = ReadRootFile("start-parking-servers.bat");

        Assert.Contains("start-parking-api.bat", script);
        Assert.Contains("start-parking-edgegateway.bat", script);
        Assert.Contains("start-parking-edgeservice.bat", script);
    }

    [Fact]
    public void Stop_script_targets_only_three_server_processes()
    {
        string script = ReadRootFile("stop-parking-servers.bat");

        Assert.Contains("Parking.Api", script);
        Assert.Contains("Parking.EdgeGateway", script);
        Assert.Contains("Parking.EdgeService", script);
        Assert.DoesNotContain("APSMain", script);
        Assert.DoesNotContain("JPXLpr", script);
    }

    private static string ReadRootFile(string fileName)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "ParkingSystem.sln")))
            current = current.Parent;
        string root = current?.FullName
            ?? throw new DirectoryNotFoundException("ParkingSystem.sln");
        return File.ReadAllText(Path.Combine(root, fileName));
    }
}
