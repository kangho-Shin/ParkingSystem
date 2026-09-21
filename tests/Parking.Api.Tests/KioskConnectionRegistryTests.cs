using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class KioskConnectionRegistryTests
{
    [Fact]
    public void 같은무인재접속은_최신연결만_유지한다()
    {
        KioskConnectionRegistry registry = new();
        registry.Register(301, "old");
        registry.Register(301, "new");

        registry.Unregister("old");

        Assert.True(registry.TryGetConnection(301, out string connectionId));
        Assert.Equal("new", connectionId);
    }
}
