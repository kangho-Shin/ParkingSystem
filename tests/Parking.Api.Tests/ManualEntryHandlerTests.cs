using Parking.Api.Features.Management;
using Parking.Central.Data;
using Parking.Contracts;

namespace Parking.Api.Tests;

public sealed class ManualEntryHandlerTests
{
    [Fact]
    public async Task 일반수동입차는_차종과_manual을_전달한다()
    {
        FakeParkingRepository general = new();
        FakePeriodRepository period = new(null);
        ManualEntryHandler handler = new(general, period);

        ManualEntryResponse response = await handler.HandleAsync(
            new ManualEntryRequest(9001, 2, 9010, 4001,
                "31가3100", DateTimeOffset.Now, 3), CancellationToken.None);

        Assert.Equal(ParkingSessionType.General, response.SessionType);
        Assert.NotNull(general.Request);
        Assert.Equal(3, general.Request.CarType);
        Assert.True(general.Request.IsManual);
        Assert.Null(period.SavedRequest);
    }

    [Fact]
    public async Task 등록수동입차는_등록저장소를_사용한다()
    {
        PeriodMember member = new(10, 9001, 2, 100, "회원", "41나4100", 2, DateTime.Today);
        FakeParkingRepository general = new();
        FakePeriodRepository period = new(member);
        ManualEntryHandler handler = new(general, period);

        ManualEntryResponse response = await handler.HandleAsync(
            new ManualEntryRequest(9001, 2, 9010, 4001,
                "41나4100", DateTimeOffset.Now, 1), CancellationToken.None);

        Assert.Equal(ParkingSessionType.Period, response.SessionType);
        Assert.Null(general.Request);
        Assert.NotNull(period.SavedRequest);
        Assert.True(period.SavedRequest.IsManual);
    }

    private sealed class FakeParkingRepository : IParkingEventRepository
    {
        public FieldEventRequest? Request { get; private set; }

        public Task<FieldEventResponse> SaveEntryAsync(
            FieldEventRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new FieldEventResponse(
                request.EventId, true, 101, "ENTRY_ACCEPTED", "입차되었습니다.", true));
        }
    }

    private sealed class FakePeriodRepository(PeriodMember? member) : IPeriodVehicleRepository
    {
        public FieldEventRequest? SavedRequest { get; private set; }

        public Task<PeriodMember?> FindMemberAsync(long siteId, int groupnum,
            string carNumber, DateTimeOffset at, CancellationToken cancellationToken) =>
            Task.FromResult(member);

        public Task<FieldEventResponse> SaveEntryAsync(FieldEventRequest request,
            PeriodMember value, CancellationToken cancellationToken)
        {
            SavedRequest = request;
            return Task.FromResult(new FieldEventResponse(
                request.EventId, true, 201, "PERIOD_ENTRY_ACCEPTED",
                "등록차량 입차가 처리되었습니다.", true));
        }

        public Task<OpenPeriodSession?> FindOpenAsync(long siteId, int groupnum,
            string carNumber, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FieldEventResponse> SaveExitAsync(ExitEventRequest request,
            OpenPeriodSession session, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
