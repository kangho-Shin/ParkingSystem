using Parking.Contracts;

namespace Parking.Domain.Tests;

public class ContractTests
{
    [Fact]
    public void FieldEventRequest_필수식별정보를_보관한다()
    {
        Guid eventId = Guid.NewGuid();
        DateTimeOffset occurredAt = new(2026, 9, 18, 0, 0, 0, TimeSpan.Zero);

        FieldEventRequest request = new(
            eventId,
            1,
            10,
            101,
            "12가3456",
            occurredAt);

        Assert.Equal(eventId, request.EventId);
        Assert.Equal(1, request.SiteId);
        Assert.Equal(10, request.LaneId);
        Assert.Equal(101, request.DeviceId);
        Assert.Equal("12가3456", request.CarNumber);
        Assert.Equal(occurredAt, request.OccurredAt);
    }
}
