using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Parking.Api.Features.Management;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public sealed class ParkingManagementControllerTests
{
    [Fact]
    public async Task 잘못된_현장키는_입차조회를_거부한다()
    {
        TestContext context = new();
        context.Controller.ControllerContext = ControllerContext("wrong-key");

        IActionResult result = await context.Controller.GetEntriesAsync(
            9001, null, null, null, null, null, 1, 200, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Equal(0, context.Repository.EntrySearchCount);
    }

    [Fact]
    public async Task 출차조회가_31일을_초과하면_거부한다()
    {
        TestContext context = new();
        context.Controller.ControllerContext = ControllerContext("valid-key");
        DateTimeOffset from = DateTimeOffset.Now.AddDays(-32);

        IActionResult result = await context.Controller.GetExitsAsync(
            9001, from, DateTimeOffset.Now, null, null, null, null,
            1, 200, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task 수동입차는_입차방향_장치가_아니면_거부한다()
    {
        TestContext context = new() { LaneValid = false };
        context.Controller.ControllerContext = ControllerContext("valid-key");

        IActionResult result = await context.Controller.CreateManualEntryAsync(
            new ManualEntryRequest(9001, 2, 9020, 4002,
                "12가1234", DateTimeOffset.Now, 1), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(context.General.Request);
    }

    private static ControllerContext ControllerContext(string key)
    {
        DefaultHttpContext http = new();
        http.Request.Headers["X-Site-Key"] = key;
        return new ControllerContext { HttpContext = http };
    }

    private sealed class TestContext
    {
        public TestContext()
        {
            Site = new FakeSiteRepository();
            Repository = new FakeManagementRepository();
            General = new FakeParkingRepository();
            Period = new FakePeriodRepository();
            Controller = new ParkingManagementController(
                Repository, Site, new FakeLaneValidator(this),
                new ManualEntryHandler(General, Period));
        }

        public bool LaneValid { get; set; } = true;
        public FakeSiteRepository Site { get; }
        public FakeManagementRepository Repository { get; }
        public FakeParkingRepository General { get; }
        public FakePeriodRepository Period { get; }
        public ParkingManagementController Controller { get; }
    }

    private sealed class FakeLaneValidator(TestContext context) : IParkingLaneDirectionValidator
    {
        public Task<bool> IsValidAsync(long siteId, int groupnum, long laneId,
            long deviceId, string eventType, CancellationToken cancellationToken) =>
            Task.FromResult(context.LaneValid);
    }

    private sealed class FakeSiteRepository : ISiteConfigurationRepository
    {
        public Task<bool> ValidateSiteKeyAsync(long siteId, string siteKey,
            CancellationToken cancellationToken) => Task.FromResult(siteKey == "valid-key");
        public Task<SiteConfiguration?> GetAsync(long siteId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SaveSiteAsync(ParkingSite site, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveLaneAsync(ParkingLane lane, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveDeviceAsync(ParkingDevice device, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveDeviceLinkAsync(ParkingDeviceLink link, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<VersionedSiteConfiguration?> GetVersionedAsync(long siteId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SaveVersionedAsync(VersionedSiteConfiguration value, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task TouchVersionAsync(long siteId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeManagementRepository : IParkingManagementRepository
    {
        public int EntrySearchCount { get; private set; }
        public Task<PagedParkingResult<ParkingManagementItem>> SearchEntriesAsync(ParkingManagementQuery query, CancellationToken cancellationToken)
        {
            EntrySearchCount++;
            return Task.FromResult(new PagedParkingResult<ParkingManagementItem>([], 1, 200, 0));
        }
        public Task<PagedParkingResult<ParkingManagementItem>> SearchExitsAsync(ParkingManagementQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedParkingResult<ParkingManagementItem>([], 1, 200, 0));
        public Task<bool> CorrectCarNumberAsync(long siteId, ParkingSessionType sessionType, long parkingSessionId, string carNumber, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeParkingRepository : IParkingEventRepository
    {
        public FieldEventRequest? Request { get; private set; }
        public Task<FieldEventResponse> SaveEntryAsync(FieldEventRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new FieldEventResponse(request.EventId, true, 1, "OK", "OK", true));
        }
    }

    private sealed class FakePeriodRepository : IPeriodVehicleRepository
    {
        public Task<PeriodMember?> FindMemberAsync(long siteId, int groupnum, string carNumber, DateTimeOffset at, CancellationToken cancellationToken) => Task.FromResult<PeriodMember?>(null);
        public Task<FieldEventResponse> SaveEntryAsync(FieldEventRequest request, PeriodMember member, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OpenPeriodSession?> FindOpenAsync(long siteId, int groupnum, string carNumber, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FieldEventResponse> SaveExitAsync(ExitEventRequest request, OpenPeriodSession session, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
