using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class EdgeManagerListMapperTests
{
    [Fact]
    public void 입차목록은_DeviceId를_장치번호와_이름으로_변환한다()
    {
        ParkingDevice device = new(4002, 9001, 9020, 402, "LPR", "출차LPR", null, true);
        EdgeEntryItem entry = new(
            Guid.NewGuid(), 304, 9001, 2, 9020, 4002, "34사5678",
            DateTimeOffset.Parse("2026-09-20T13:00:00+09:00"), "in.jpg",
            EdgeDeliveryState.Completed, "ENTRY_ACCEPTED");

        EdgeEntryListItem row = Assert.Single(
            EdgeManagerListMapper.MapEntries(new[] { entry }, new[] { device }));

        Assert.Equal(402, row.DeviceNumber);
        Assert.Equal("출차LPR", row.DeviceName);
        Assert.Equal("34사5678", row.CarNumber);
        Assert.Equal(entry.InDateTime, row.InDateTime);
    }

    [Fact]
    public void 처리목록은_DeviceId를_장치번호와_이름으로_변환한다()
    {
        ParkingDevice device = new(4002, 9001, 9020, 402, "LPR", "출차LPR", null, true);
        EdgeActivityItem activity = new(
            Guid.NewGuid(), EdgeActivityType.Exit, 304, 9001, 2, 9020, 4002,
            "34사5678", DateTimeOffset.Parse("2026-09-20T13:10:00+09:00"),
            "in.jpg", "out.jpg", null, null, null, null, true,
            EdgeDeliveryState.Completed, "EXIT_ACCEPTED");

        EdgeActivityListItem row = Assert.Single(
            EdgeManagerListMapper.MapActivities(new[] { activity }, new[] { device }));

        Assert.Equal(402, row.DeviceNumber);
        Assert.Equal("출차LPR", row.DeviceName);
        Assert.Equal("34사5678", row.CarNumber);
        Assert.Equal(activity.OccurredAt, row.OccurredAt);
    }
}
