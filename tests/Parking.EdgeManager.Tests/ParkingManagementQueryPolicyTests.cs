using Parking.EdgeManager.Core;

namespace Parking.EdgeManager.Tests;

public sealed class ParkingManagementQueryPolicyTests
{
    [Fact]
    public void 출차조회는_31일을_초과할수없다()
    {
        DateTimeOffset from = new(2026, 8, 1, 0, 0, 0, TimeSpan.FromHours(9));
        DateTimeOffset to = from.AddDays(32);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(
            () => ParkingManagementQueryPolicy.ValidateExitRange(from, to));

        Assert.Equal("to", error.ParamName);
    }

    [Fact]
    public void 종료일이_시작일보다_빠르면_거부한다()
    {
        DateTimeOffset from = new(2026, 9, 23, 0, 0, 0, TimeSpan.FromHours(9));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ParkingManagementQueryPolicy.ValidateExitRange(from, from.AddMinutes(-1)));
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(600, 500)]
    [InlineData(25, 25)]
    public void 페이지크기를_1에서_500사이로_정규화한다(int value, int expected) =>
        Assert.Equal(expected, ParkingManagementQueryPolicy.NormalizePageSize(value));
}
