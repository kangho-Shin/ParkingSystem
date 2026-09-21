using Parking.Contracts;
using Parking.EdgeService;

namespace Parking.Api.Tests;

public sealed class EdgeDeviceResolverTests
{
    [Fact]
    public void 현재_현장과_그룹의_KIOSK_장비번호를_DeviceId로_변환한다()
    {
        ParkingDevice device = EdgeDeviceResolver.Resolve(
            Configuration(), new EdgeDeviceIdentity(9001, 2, 201, "KIOSK"));

        Assert.Equal(2001, device.DeviceId);
    }

    [Theory]
    [InlineData(9002, 2, 201, "INVALID_SITE")]
    [InlineData(9001, 1, 201, "DEVICE_NOT_FOUND")]
    [InlineData(9001, 2, 202, "DEVICE_NOT_FOUND")]
    public void 다른_현장_그룹_장비번호는_거부한다(
        long sitenum, int groupnum, int devicenum, string expectedCode)
    {
        DeviceIdentityException error = Assert.Throws<DeviceIdentityException>(() =>
            EdgeDeviceResolver.Resolve(
                Configuration(), new EdgeDeviceIdentity(sitenum, groupnum, devicenum, "KIOSK")));

        Assert.Equal(expectedCode, error.Code);
    }

    [Fact]
    public void 장비종류가_다르면_거부한다()
    {
        DeviceIdentityException error = Assert.Throws<DeviceIdentityException>(() =>
            EdgeDeviceResolver.Resolve(
                Configuration(), new EdgeDeviceIdentity(9001, 2, 201, "LPR")));

        Assert.Equal("DEVICE_NOT_FOUND", error.Code);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void 비활성_차로_또는_장비는_거부한다(bool laneEnabled, bool deviceEnabled)
    {
        DeviceIdentityException error = Assert.Throws<DeviceIdentityException>(() =>
            EdgeDeviceResolver.Resolve(
                Configuration(laneEnabled, deviceEnabled),
                new EdgeDeviceIdentity(9001, 2, 201, "KIOSK")));

        Assert.Equal("DEVICE_NOT_FOUND", error.Code);
    }

    [Fact]
    public void 같은_식별값이_둘이면_거부한다()
    {
        SiteConfiguration configuration = Configuration();
        configuration = configuration with
        {
            Devices = configuration.Devices.Concat(new[]
            {
                new ParkingDevice(2002, 9001, 9020, 201, "KIOSK", "중복무인", null, true)
            }).ToList()
        };

        DeviceIdentityException error = Assert.Throws<DeviceIdentityException>(() =>
            EdgeDeviceResolver.Resolve(
                configuration, new EdgeDeviceIdentity(9001, 2, 201, "KIOSK")));

        Assert.Equal("DEVICE_AMBIGUOUS", error.Code);
    }

    private static SiteConfiguration Configuration(
        bool laneEnabled = true, bool deviceEnabled = true) => new(
        new ParkingSite(9001, "시험주차장", true),
        new[] { new ParkingLane(9020, 9001, 2, "출차", "Exit", laneEnabled) },
        new[] { new ParkingDevice(2001, 9001, 9020, 201, "KIOSK", "출구무인", null, deviceEnabled) });
}
