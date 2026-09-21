using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class DisplayBoardSelectorTests
{
    [Fact]
    public void 같은차로에서도_LPR카메라_DeviceId에_연결된전광판을_선택한다()
    {
        SiteConfiguration configuration = new(
            new ParkingSite(1, "시험현장", true),
            new[] { new ParkingLane(10, 1, 1, "입구", "Entry", true) },
            new ParkingDevice[]
            {
                new(101, 1, 10, 101, "LPR", "카메라1", null, true),
                new(102, 1, 10, 102, "LPR", "카메라2", null, true),
                new(201, 1, 10, 201, "LDM", "전광판1", "192.168.0.201", true, 3000),
                new(202, 1, 10, 202, "LDM", "전광판2", "192.168.0.202", true, 3000)
            },
            new[]
            {
                new ParkingDeviceLink(1, 101, 201, "LDM", true),
                new ParkingDeviceLink(1, 102, 202, "LDM", true)
            });

        ParkingDevice? display = DisplayBoardSelector.Find(configuration, 102);

        Assert.Equal(202, display!.DeviceId);
    }
}
