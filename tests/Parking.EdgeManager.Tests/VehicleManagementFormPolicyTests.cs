using Parking.Contracts;
using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class VehicleManagementFormPolicyTests
{
    [Fact]
    public void 입차장치목록은_활성_ENTRY차로의_LPR만_포함한다()
    {
        SiteConfiguration configuration = Configuration();

        IReadOnlyList<ParkingDevice> result =
            VehicleManagementFormPolicy.EntryDevices(configuration);

        Assert.Equal(new long[] { 4001 }, result.Select(x => x.DeviceId));
    }

    [Fact]
    public void 중앙연결실패후에도_다시조회할수있다()
    {
        VehicleManagementButtonState state =
            VehicleManagementFormPolicy.AfterRequestFailed();

        Assert.True(state.SearchEnabled);
        Assert.True(state.CloseEnabled);
    }

    [Fact]
    public void 출차화면_기본기간은_오늘자정부터_현재까지다()
    {
        DateTimeOffset now = new(2026, 9, 23, 14, 30, 0, TimeSpan.FromHours(9));

        (DateTimeOffset from, DateTimeOffset to) =
            VehicleManagementFormPolicy.DefaultExitRange(now);

        Assert.Equal(new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.FromHours(9)), from);
        Assert.Equal(now, to);
    }

    [Fact]
    public void 중앙연결정보가_없으면_관리버튼만_비활성화한다() =>
        Assert.False(VehicleManagementFormPolicy.ManagementEnabled(null));

    private static SiteConfiguration Configuration() => new(
        new ParkingSite(9001, "시험현장", true),
        [
            new ParkingLane(9010, 9001, 1, "입차", "ENTRY", true),
            new ParkingLane(9020, 9001, 2, "출차", "EXIT", true),
            new ParkingLane(9030, 9001, 3, "정지", "ENTRY", false)
        ],
        [
            new ParkingDevice(4001, 9001, 9010, 401, "LPR", "입차LPR", null, true),
            new ParkingDevice(4002, 9001, 9020, 402, "LPR", "출차LPR", null, true),
            new ParkingDevice(4003, 9001, 9010, 403, "KIOSK", "키오스크", null, true),
            new ParkingDevice(4004, 9001, 9030, 404, "LPR", "중지LPR", null, true)
        ]);
}
