using APSMain.Integration.EdgeService;

namespace APSMain.EdgeIntegration.Tests;

public sealed class KioskSignalRClientTests
{
    [Fact]
    public async Task 최초연결과_재연결에_사용할_외부식별값을_보관한다()
    {
        EdgeServiceOptions options = new(
            new Uri("http://localhost:5200/"), 9001, 2, 201);
        await using KioskSignalRClient client = new(options);

        Assert.Equal((9001L, 2, 201), client.RegistrationIdentity);
    }
}
