using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeOperationPolicyTests
{
    [Theory]
    [InlineData("BLOCK", KioskOfflinePolicy.Block)]
    [InlineData("block", KioskOfflinePolicy.Block)]
    [InlineData("OPEN", KioskOfflinePolicy.Open)]
    [InlineData("invalid", KioskOfflinePolicy.Open)]
    [InlineData(null, KioskOfflinePolicy.Open)]
    public void 무인장애정책은_그룹별값을_해석한다(string? value, KioskOfflinePolicy expected)
    {
        SiteConfiguration configuration = new(
            new ParkingSite(1, "시험", true), Array.Empty<ParkingLane>(),
            Array.Empty<ParkingDevice>(), OperationVariables: value is null
                ? Array.Empty<ParkingOperationVariable>()
                : new[] { new ParkingOperationVariable(2, "CMD_KIOSK_OFFLINE_POLICY", value) });

        Assert.Equal(expected, KioskOfflinePolicyResolver.Resolve(configuration, 2));
    }
}
