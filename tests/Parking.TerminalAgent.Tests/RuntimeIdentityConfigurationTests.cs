namespace Parking.TerminalAgent.Tests;

public sealed class RuntimeIdentityConfigurationTests
{
    [Fact]
    public void 활성_설정과_실행파일은_현장_그룹_장비번호를_사용한다()
    {
        string root = FindRepositoryRoot();
        string[] files =
        {
            "run-foundations.bat",
            "start-ldm-test.bat",
            "run-terminal-agent-test.bat",
            "run-apsmain-edge-test.bat",
            "APSMain_C(API)V2/App.config",
            "src/Edge/Parking.TerminalAgent/appsettings.json"
        };

        string text = string.Join("\n", files.Select(path =>
            File.ReadAllText(Path.Combine(root, path))));

        Assert.DoesNotContain("APSDEVICEID", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("key=\"SITEID\"", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--device 2001", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--site 9001 --group 2 --device-number 201", text);
    }

    [Fact]
    public void APSMain_전체시험은_빌드전에_실행중인_프로그램을_종료한다()
    {
        string root = FindRepositoryRoot();
        string batch = File.ReadAllText(Path.Combine(root, "run-apsmain-edge-test.bat"));

        Assert.Contains("Parking.Operator", batch);
        Assert.Contains("Parking.EdgeService", batch);
        Assert.Contains("Stop-Process -Force", batch);
        Assert.True(
            batch.IndexOf("Stop-Process -Force", StringComparison.OrdinalIgnoreCase) <
            batch.IndexOf("dotnet restore", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void APSMain_전체시험은_시험데이터가_있는_DB를_기본으로_사용한다()
    {
        string root = FindRepositoryRoot();
        string batch = File.ReadAllText(Path.Combine(root, "run-apsmain-edge-test.bat"));

        Assert.Contains("database=parking000test", batch, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database=parking;", batch, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ParkingSystem.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("저장소 최상위 디렉터리를 찾을 수 없습니다.");
    }
}
