using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeDataDirectoryTests
{
    [Fact]
    public void 설정경로가_없으면_실행폴더의_Data를_사용한다()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), "Parking.EdgeService");

        string result = EdgeDataDirectory.Resolve(null, baseDirectory);

        Assert.Equal(Path.Combine(baseDirectory, "Data"), result);
    }

    [Fact]
    public void 설정경로가_있으면_그_경로를_우선한다()
    {
        string configuredDirectory = Path.Combine(Path.GetTempPath(), "ParkingData");

        string result = EdgeDataDirectory.Resolve(configuredDirectory, "ignored");

        Assert.Equal(configuredDirectory, result);
    }
}
