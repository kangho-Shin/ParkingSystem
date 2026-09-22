namespace Parking.TerminalAgent.Tests;

public sealed class APSMainSourceBoundaryTests
{
    [Fact]
    public void APSMain은_DB_직접연결_패키지를_사용하지_않는다()
    {
        string root = FindRepositoryRoot();
        string project = File.ReadAllText(Path.Combine(root, "APSMain_C(API)V2", "APSMain.csproj"));

        foreach (string name in new[] { "EntityFrameworkCore", "Pomelo", "MySql.Data", "Dapper" })
            Assert.DoesNotContain(name, project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void APSMain에는_DB_생성모델과_직접작업자가_없다()
    {
        string root = FindRepositoryRoot();
        string aps = Path.Combine(root, "APSMain_C(API)V2");

        Assert.False(Directory.Exists(Path.Combine(aps, "DbModels")));
        Assert.False(File.Exists(Path.Combine(aps, "BaseClass", "DbJobWorker.cs")));
    }

    [Fact]
    public void APSMain에는_하드코딩_DB_접속정보가_없다()
    {
        string root = FindRepositoryRoot();
        string aps = Path.Combine(root, "APSMain_C(API)V2");
        string text = string.Join("\n", Directory.EnumerateFiles(aps, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj" or ".config")
            .Select(File.ReadAllText));

        Assert.DoesNotContain("database=parking000test", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("database=test", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password=test", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MySqlConnection", text, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ParkingSystem.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("저장소 최상위 디렉터리를 찾을 수 없습니다.");
    }
}
