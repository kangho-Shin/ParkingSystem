using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class DeviceLinkSelectorTests
{
    [Fact]
    public void 활성연결과_대상장비종류가_일치할때만_선택한다()
    {
        SiteConfiguration configuration = new(
            new ParkingSite(1, "시험", true), Array.Empty<ParkingLane>(),
            new ParkingDevice[]
            {
                new(101, 1, 10, 101, "LPR", "카메라", null, true),
                new(301, 1, 10, 301, "KIOSK", "무인", null, true),
                new(302, 1, 10, 302, "KIOSK", "중지무인", null, false),
                new(401, 1, 10, 401, "LDM", "전광판", null, true)
            },
            new ParkingDeviceLink[]
            {
                new(1, 101, 301, "KIOSK", true),
                new(1, 101, 302, "KIOSK", true),
                new(1, 101, 401, "KIOSK", true),
                new(1, 101, 101, "KIOSK", true)
            });

        IReadOnlyList<ParkingDevice> targets = DeviceLinkSelector.FindTargets(
            configuration, 101, "KIOSK", "KIOSK");

        Assert.Equal(new long[] { 301 }, targets.Select(device => device.DeviceId));
    }
}
