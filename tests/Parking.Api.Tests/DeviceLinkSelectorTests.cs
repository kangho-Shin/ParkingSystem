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

    [Fact]
    public void 출구무인에_연결된_출차LPR을_역방향으로_찾는다()
    {
        SiteConfiguration configuration = new(
            new ParkingSite(9001, "시험", true),
            new[] { new ParkingLane(9020, 9001, 2, "출차", "Exit", true) },
            new ParkingDevice[]
            {
                new(4002, 9001, 9020, 402, "LPR", "출차LPR", null, true),
                new(2001, 9001, 9020, 201, "KIOSK", "출구무인", null, true)
            },
            new[] { new ParkingDeviceLink(9001, 4002, 2001, "KIOSK", true) });

        ParkingDevice? lpr = DeviceLinkSelector.FindSource(
            configuration, 2001, "KIOSK", "LPR");

        Assert.Equal(4002, lpr!.DeviceId);
        Assert.Equal(9020, lpr.LaneId);
    }
}
