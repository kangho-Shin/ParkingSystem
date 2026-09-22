using Newtonsoft.Json;
using Parking.Contracts;
using Parking.EdgeService;
using Microsoft.AspNetCore.Mvc;

namespace Parking.Api.Tests;

public sealed class KioskDeviceContractTests
{
    [Fact]
    public void Kiosk_외부식별계약에는_DeviceId가_없다()
    {
        string json = JsonConvert.SerializeObject(new KioskDeviceIdentity(9001, 2, 201));

        Assert.Equal("{\"Sitenum\":9001,\"Groupnum\":2,\"Devicenum\":201}", json);
        Assert.DoesNotContain("DeviceId", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task 완료요청은_외부식별값을_내부_DeviceId로_변환한다()
    {
        FakeDeviceResolver resolver = new(new ParkingDevice(
            2001, 9001, 9020, 201, "KIOSK", "출구무인", null, true));
        FakeKioskExitCoordinator coordinator = new(new FieldEventResponse(
            Guid.Empty, true, null, "COMPLETED", "완료", false));
        KioskEventController controller = new(coordinator, resolver);
        Guid eventId = Guid.NewGuid();

        IActionResult result = await controller.CompleteAsync(
            eventId, new KioskDeviceIdentity(9001, 2, 201), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(2001, coordinator.DeviceId);
        Assert.Equal(eventId, coordinator.EventId);
        Assert.Equal(new EdgeDeviceIdentity(9001, 2, 201, "KIOSK"), resolver.Identity);
    }

    [Fact]
    public async Task 수동정산_표시요청은_Kiosk_연결_전광판으로_전달한다()
    {
        FakeDeviceResolver resolver = new(new ParkingDevice(
            2001, 9001, 9020, 201, "KIOSK", "출구무인", null, true));
        FakeKioskExitCoordinator coordinator = new(null);
        KioskEventController controller = new(coordinator, resolver);

        IActionResult result = await controller.DisplayAsync(
            new KioskDisplayRequest(
                new KioskDeviceIdentity(9001, 2, 201),
                "12가3456", "정산 완료되었습니다."),
            CancellationToken.None);

        Assert.IsType<OkResult>(result);
        Assert.Equal(2001, coordinator.DeviceId);
        Assert.Equal("12가3456", coordinator.CarNumber);
        Assert.Equal("정산 완료되었습니다.", coordinator.DisplayMessage);
        Assert.Equal(11, coordinator.DisplaySeconds);
    }

    [Fact]
    public async Task 다른_Kiosk의_사건이면_NotFound를_반환한다()
    {
        FakeDeviceResolver resolver = new(new ParkingDevice(
            2002, 9001, 9020, 202, "KIOSK", "다른무인", null, true));
        FakeKioskExitCoordinator coordinator = new(null);
        KioskEventController controller = new(coordinator, resolver);

        IActionResult result = await controller.CompleteAsync(
            Guid.NewGuid(), new KioskDeviceIdentity(9001, 2, 202), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(2002, coordinator.DeviceId);
    }

    [Theory]
    [InlineData(0, 2, 201)]
    [InlineData(9001, 0, 201)]
    [InlineData(9001, 2, 0)]
    public async Task 잘못된_외부식별값은_BadRequest를_반환한다(
        long sitenum, int groupnum, int devicenum)
    {
        KioskEventController controller = new(
            new FakeKioskExitCoordinator(null),
            new FakeDeviceResolver(new ParkingDevice(
                2001, 9001, 9020, 201, "KIOSK", "출구무인", null, true)));

        IActionResult result = await controller.CompleteAsync(
            Guid.NewGuid(), new KioskDeviceIdentity(sitenum, groupnum, devicenum), CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
    }

    private sealed class FakeDeviceResolver(ParkingDevice device) : IEdgeDeviceResolver
    {
        public EdgeDeviceIdentity? Identity { get; private set; }
        public Task<ParkingDevice> ResolveAsync(EdgeDeviceIdentity identity, CancellationToken cancellationToken)
        {
            Identity = identity;
            return Task.FromResult(device);
        }
    }

    private sealed class FakeKioskExitCoordinator(FieldEventResponse? response) : IKioskExitCoordinator
    {
        public long DeviceId { get; private set; }
        public Guid EventId { get; private set; }
        public string? CarNumber { get; private set; }
        public string? DisplayMessage { get; private set; }
        public int DisplaySeconds { get; private set; }
        public Task<FieldEventResponse?> CompleteAsync(
            long kioskDeviceId, Guid eventId, CancellationToken cancellationToken)
        {
            DeviceId = kioskDeviceId;
            EventId = eventId;
            return Task.FromResult(response);
        }

        public Task DisplayAsync(
            long kioskDeviceId, long siteId, int groupnum,
            string carNumber, string displayMessage,
            int displaySeconds,
            CancellationToken cancellationToken)
        {
            DeviceId = kioskDeviceId;
            CarNumber = carNumber;
            DisplayMessage = displayMessage;
            DisplaySeconds = displaySeconds;
            return Task.CompletedTask;
        }

        public Task ResetDisplayAsync(
            long kioskDeviceId, long siteId, CancellationToken cancellationToken)
        {
            DeviceId = kioskDeviceId;
            return Task.CompletedTask;
        }

        public Task<FieldEventResponse?> CompleteManualAsync(
            long kioskDeviceId, long siteId, int groupnum, string carNumber,
            DateTimeOffset exitAt, CancellationToken cancellationToken)
        {
            DeviceId = kioskDeviceId;
            CarNumber = carNumber;
            return Task.FromResult(response);
        }
    }
}
